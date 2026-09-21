using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.GuildCity017H;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.SpecialRelic001;
using SecondDimension.Gameplay.SSSTenV4;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.Campaign022
{
    public sealed partial class CampaignProgressionCommandService022
    {
        public const string GreatCovenantsStoryGate="STORY_GATE_GREAT_COVENANTS";
        public const int TowerMajorRecruitInterval089=50;
        public const int MaximumTowerThreatTier089=10;
        const int CovenantTrialStageProgress=25;
        const int CovenantTrialTrustPerStage=6;
        static readonly string[] TowerMajorRecruitRoster089=
        {
            "HERO_REC_011","HERO_REC_014","HERO_REC_017","HERO_REC_021",
            "HERO_REC_024","HERO_REC_028","HERO_REC_031","HERO_REC_034",
            "HERO_REC_038","HERO_REC_041","HERO_REC_044","HERO_REC_048",
            "HERO_REC_051","HERO_REC_055","HERO_REC_058","HERO_REC_061",
            "HERO_REC_065","HERO_REC_068","HERO_REC_071","HERO_REC_075"
        };

        public Result<CampaignState> RecordEquipmentMeaningfulUse(CampaignState campaign,ICampaignRegistry022 registry,string itemInstanceId,string trackId,int masteryPoints,string historyTag)
        {
            if(!TryContext(campaign,out var city,out var strategic,out var progress,out var playable,out var state,out var error)||registry==null)return Result<CampaignState>.Failure(error??"CAMPAIGN022_INPUT_REQUIRED");if(!campaign.Guild.Inventory.Any(x=>StringComparer.Ordinal.Equals(x.InstanceId,itemInstanceId)))return Result<CampaignState>.Failure("CAMPAIGN022_ITEM_NOT_OWNED");if(!registry.WeaponTracks.ContainsKey(trackId))return Result<CampaignState>.Failure("CAMPAIGN022_TRACK_UNKNOWN");if(masteryPoints<=0)return Result<CampaignState>.Failure("CAMPAIGN022_MEANINGFUL_USE_REQUIRED");var list=new List<EquipmentEvolutionState022>(state.EquipmentEvolution);var idx=list.FindIndex(x=>StringComparer.Ordinal.Equals(x.ItemInstanceId,itemInstanceId));var current=idx>=0?list[idx]:new EquipmentEvolutionState022(itemInstanceId,trackId,"TRAINING",0,0,Array.Empty<string>(),string.Empty);if(!StringComparer.Ordinal.Equals(current.TrackId,trackId))return Result<CampaignState>.Failure("CAMPAIGN022_ITEM_TRACK_IMMUTABLE");current=current.With(meaningfulUses:current.MeaningfulUses+1,masteryPoints:checked(current.MasteryPoints+masteryPoints),historyTag:historyTag??current.HistoryTag);if(idx>=0)list[idx]=current;else list.Add(current);return Success(campaign,city,strategic,progress,playable,state.With(equipmentEvolution:list.AsReadOnly(),lastCheckpointId:"campaign022_equipment_use"));
        }

        public Result<CampaignState> EvolveWeapon(CampaignState campaign,ICampaignRegistry022 registry,string itemInstanceId,string recipeId)
        { return EvolveWeaponWithCombat154(campaign,registry,itemInstanceId,recipeId); }

        public Result<CampaignState> CertifyAdvancedClass(CampaignState campaign,ICampaignRegistry022 registry,string recruitId,string classId,int primaryProficiency,int secondaryProficiency,int primaryUnionRank,int secondaryUnionRank,int meaningfulPrimary,int meaningfulSecondary)
        {
            if(!TryContext(campaign,out var city,out var strategic,out var progress,out var playable,out var state,out var error)||registry==null)return Result<CampaignState>.Failure(error??"CAMPAIGN022_INPUT_REQUIRED");if(!campaign.Guild.Recruits.Any(x=>StringComparer.Ordinal.Equals(x.RecruitId,recruitId)&&x.AuthorityKind==RecruitAuthorityKind.Normal))return Result<CampaignState>.Failure("CAMPAIGN022_NORMAL_RECRUIT_REQUIRED");if(!registry.Classes.TryGetValue(classId,out var profile))return Result<CampaignState>.Failure("CAMPAIGN022_CLASS_UNKNOWN");if(primaryProficiency<profile.primaryProficiencyRequired||secondaryProficiency<profile.secondaryProficiencyRequired||primaryUnionRank<profile.primaryUnionRankRequired||secondaryUnionRank<profile.secondaryUnionRankRequired||meaningfulPrimary<profile.meaningfulPrimaryActionsRequired||meaningfulSecondary<profile.meaningfulSecondaryActionsRequired)return Result<CampaignState>.Failure("CAMPAIGN022_CLASS_REQUIREMENTS_NOT_MET");var receipt="CLASSREC022_"+CanonicalJson.Sha256Hex(new{campaign.CampaignGuid,recruitId,classId,primaryProficiency,secondaryProficiency}).Substring(0,24).ToUpperInvariant();var list=new List<ClassCertificationState022>(state.ClassCertifications);var idx=list.FindIndex(x=>StringComparer.Ordinal.Equals(x.RecruitId,recruitId));var current=idx>=0?list[idx]:new ClassCertificationState022(recruitId,string.Empty,Array.Empty<string>(),Array.Empty<string>());if(current.AppliedCertificationReceiptIds.Contains(receipt))return Result<CampaignState>.Success(campaign);var classes=new List<string>(current.CertifiedClassIds);if(!classes.Contains(classId))classes.Add(classId);var receipts=new List<string>(current.AppliedCertificationReceiptIds){receipt};current=current.With(activeAdvancedClassId:classId,certifiedClassIds:classes.AsReadOnly(),appliedCertificationReceiptIds:receipts.AsReadOnly());if(idx>=0)list[idx]=current;else list.Add(current);return Success(campaign,city,strategic,progress,playable,state.With(classCertifications:list.AsReadOnly(),lastCheckpointId:"campaign022_class_certified"));
        }

        public Result<CampaignState> BeginAbyssOperation(CampaignState campaign,ICampaignRegistry022 registry,string operationId)
        {
            // Once the player has entered the new floor policy, new UI starts
            // must bind an actual floor. Existing pending legacy runs can finish.
            if(campaign?.Guild?.Development?.AppliedAdventureAuthorityIds.Any(
                   value=>value.StartsWith(TowerFloorBindingPrefix094,StringComparison.Ordinal))==true)
                return Result<CampaignState>.Failure("TOWER094_USE_ENDLESS_FLOOR_START");
            return BeginAbyssOperationCore094(campaign,registry,operationId);
        }

        Result<CampaignState> BeginAbyssOperationCore094(CampaignState campaign,ICampaignRegistry022 registry,string operationId)
        {
            if(SecondDimension.Gameplay.Navigation164.LoopCheckpoint164.HasParked(campaign,"TOWER"))return Result<CampaignState>.Failure("Resume the saved Tower floor before starting another floor.");
            if(!TryContext(campaign,out var city,out var strategic,out var progress,out var playable,out var state,out var error)||registry==null)return Result<CampaignState>.Failure(error??"CAMPAIGN022_INPUT_REQUIRED");
            if(!StringComparer.Ordinal.Equals(state.ContentAuthorityVersion,
                   CampaignProgressionState022.ContentVersion))
                return Result<CampaignState>.Failure(
                    "CAMPAIGN022_LEGACY_RECOVERY_REQUIRED");
            state=EnsureAbyssAuthority084(campaign,state);
            if(!ValidateAbyssAuthority084(campaign,registry,state))
                return Result<CampaignState>.Failure(
                    "CAMPAIGN022_ABYSS_AUTHORITY_LEDGER_INVALID");
            if(GuildCityExpeditionService017D.HasAnyUnresolvedAdventure084(campaign, allowPausedCampaign150:true))
                return Result<CampaignState>.Failure(
                    "CAMPAIGN022_FINISH_ACTIVE_GUILD_ADVENTURE_FIRST");
            if(state.ActiveAbyssOperation!=null)return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_OPERATION_ACTIVE");
            if(!registry.AbyssOperations.TryGetValue(operationId,out var op))return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_OPERATION_UNKNOWN");
            var lawError=ValidateAbyssOperationLaw(registry,op,out var floor);if(lawError!=null)return Result<CampaignState>.Failure(lawError);
            if(!StringComparer.Ordinal.Equals(op.operationId,operationId))return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_AUTHORED_LAW_INVALID");
            if(op.requiresPreviousFloorClear&&floor.floor>1)
            {
                var previousFloors=registry.Floors.Values.Where(x=>x.floor==floor.floor-1).Take(2).ToArray();
                if(previousFloors.Length!=1||!state.AbyssFloors.Any(x=>StringComparer.Ordinal.Equals(x.FloorId,previousFloors[0].floorId)&&x.ClearCount>0&&x.FirstClearApplied))return Result<CampaignState>.Failure("CAMPAIGN022_PREVIOUS_FLOOR_REQUIRED");
            }
            var currentFloor=state.AbyssFloors.FirstOrDefault(x=>
                StringComparer.Ordinal.Equals(x.FloorId,op.floorId));
            if(!op.firstClearOnly&&(currentFloor==null||currentFloor.ClearCount<=0||
                !currentFloor.FirstClearApplied))
                return Result<CampaignState>.Failure("CAMPAIGN022_GUARDIAN_FIRST_CLEAR_REQUIRED");
            if(op.firstClearOnly&&state.AbyssFloors.Any(x=>StringComparer.Ordinal.Equals(x.FloorId,op.floorId)&&x.ClearCount>0))return Result<CampaignState>.Failure("CAMPAIGN022_FIRST_CLEAR_ALREADY_COMPLETE");
            if(!TryAbyssRosterSnapshot084(campaign,out var rosterIdentity,
                   out var alliedRecruitIds))
                return Result<CampaignState>.Failure(
                    "CAMPAIGN022_ALLIED_ROSTER_AUTHORITY_INVALID");
            var receiptCountAtBegin=TowerReceiptIds084(state.AppliedReceiptIds).Count;
            var floorClearCountAtBegin=currentFloor?.ClearCount??0;
            var committedOrdinal=checked(city.OperationOrdinal+1);
            var beginAuthority=CreateAbyssBeginAuthority084(campaign,operationId,
                op.floorId,committedOrdinal,state.AbyssAuthorityHash,
                receiptCountAtBegin,floorClearCountAtBegin,rosterIdentity,
                alliedRecruitIds);
            var seed=CreateAbyssCanonicalSeed(campaign,operationId,
                receiptCountAtBegin,committedOrdinal,state.AbyssAuthorityHash,
                floorClearCountAtBegin,rosterIdentity,alliedRecruitIds,
                beginAuthority);
            var active=new AbyssOperationState022(
                "ABYSSRUN022_"+seed.Substring(0,24).ToUpperInvariant(),operationId,
                op.floorId,0,AbyssOperationStatus022.Active,Array.Empty<string>(),
                null,string.Empty,seed,receiptCountAtBegin,
                Array.Empty<AbyssStepReceiptProof022>(),committedOrdinal,
                state.AbyssAuthorityHash,beginAuthority,rosterIdentity,
                alliedRecruitIds,floorClearCountAtBegin);
            var beginGrant=new AbyssBeginGrant022(beginAuthority,
                state.AbyssAuthorityHash,committedOrdinal,
                active.OperationInstanceId,operationId,op.floorId,
                receiptCountAtBegin,floorClearCountAtBegin,rosterIdentity,
                alliedRecruitIds);
            city=city.With(operationOrdinal:committedOrdinal,
                lastCheckpointId:"campaign022_abyss_started");
            return Success(campaign,city,strategic,progress,playable,state.With(activeAbyssOperation:active,replaceActiveAbyssOperation:true,lastCheckpointId:"campaign022_abyss_started",activeAbyssBeginGrant:beginGrant,replaceActiveAbyssBeginGrant:true,activeAbyssStepLedger:Array.Empty<AbyssStepReceiptProof022>()));
        }

        public Result<CampaignState> RetreatAbyssOperation(CampaignState campaign,ICampaignRegistry022 registry)
            =>RetreatAbyssOperationCore130(campaign,registry,null);

        Result<CampaignState> RetreatAbyssOperationCore130(CampaignState campaign,ICampaignRegistry022 registry,TowerRestartDefeatProof130 restartProof)
        {
            if(!TryContext(campaign,out var city,out var strategic,out var progress,out var playable,out var state,out var error)||registry==null)
                return Result<CampaignState>.Failure(error??"CAMPAIGN022_INPUT_REQUIRED");
            if(state.ActiveAbyssOperation==null)
                return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_ACTIVE_REQUIRED");
            if(!HasCanonicalActiveAbyssIdentity(campaign,registry,
                   state.ActiveAbyssOperation))
                return Result<CampaignState>.Failure(
                    "CAMPAIGN022_ABYSS_ACTIVE_AUTHORITY_INVALID");
            if(campaign?.Battle?.Outcome==BattleOutcome.InProgress&&HasMatchingActiveAbyssBattle(campaign,registry))
                return Result<CampaignState>.Failure("CAMPAIGN022_ACTIVE_BATTLE_MUST_FINISH");
            city=city.With(
                pendingEncounter:null,
                replacePendingEncounter:true,
                pendingBattleReturn:null,
                replacePendingBattleReturn:true,
                lastCheckpointId:"campaign022_tower_retreat");
            var abortEntry=CreateAbyssAbortEntry084(state,
                state.ActiveAbyssOperation,restartProof);
            if(abortEntry==null)return Result<CampaignState>.Failure(
                "CAMPAIGN022_ABYSS_ABORT_AUTHORITY_INVALID");
            var authorityEntries=new List<AbyssAuthorityEntry022>(
                state.AbyssAuthorityEntries){abortEntry};
            state=state.With(
                activeAbyssOperation:null,
                replaceActiveAbyssOperation:true,
                lastCheckpointId:"campaign022_tower_retreat",
                activeAbyssBeginGrant:null,replaceActiveAbyssBeginGrant:true,
                activeAbyssStepLedger:Array.Empty<AbyssStepReceiptProof022>(),
                abyssAuthorityEntries:authorityEntries.AsReadOnly(),
                abyssAuthorityHash:abortEntry.EntryHash);
            return Success(campaign,city,strategic,progress,playable,state);
        }

        public static bool RequiresLegacyAbyssRecovery084(
            CampaignProgressionState022 state)=>state!=null&&
            StringComparer.Ordinal.Equals(state.ContentAuthorityVersion,
                "CAMPAIGN_PROGRESSION_ABYSS_COVENANT_022_1.0")&&
            string.IsNullOrWhiteSpace(state.AbyssAuthorityBaseHash)&&
            string.IsNullOrWhiteSpace(state.AbyssAuthorityHash)&&
            state.AbyssAuthorityEntries.Count==0&&
            string.IsNullOrWhiteSpace(state.AbyssAuthorityMigrationReceiptId);

        public Result<CampaignState> MigrateInactiveLegacyAbyssState084(
            CampaignState campaign)
        {
            if(!TryContext(campaign,out var city,out var strategic,
                   out var progress,out var playable,out var state,out var error))
                return Result<CampaignState>.Failure(error??
                    "CAMPAIGN022_INPUT_REQUIRED");
            if(!RequiresLegacyAbyssRecovery084(state))
                return Result<CampaignState>.Failure(
                    "CAMPAIGN022_LEGACY_RECOVERY_NOT_REQUIRED");
            if(state.ActiveAbyssOperation!=null||city.PendingEncounter!=null||
               city.PendingBattleReturn!=null)
                return Result<CampaignState>.Failure(
                    "CAMPAIGN022_LEGACY_ACTIVE_RECOVERY_CONFIRMATION_REQUIRED");
            var towerReceipts=TowerReceiptIds084(state.AppliedReceiptIds);
            var migrationReceipt=AbyssMigrationReceiptId084(campaign,
                city.OperationOrdinal,state.AbyssFloors,towerReceipts,
                state.AboveGroundChangeIds);
            var root=AbyssAuthorityRootHash084(campaign,city.OperationOrdinal,
                state.AbyssFloors,towerReceipts,state.AboveGroundChangeIds,
                migrationReceipt);
            var migrated=new CampaignProgressionState022(
                CampaignProgressionState022.ContentVersion,state.EquipmentEvolution,
                state.ClassCertifications,state.AbyssFloors,null,
                state.InvocationArtifacts,state.Covenants,state.AboveGroundChangeIds,
                state.AppliedReceiptIds,state.SummonResonance,
                "campaign022_legacy_tower_migrated",root,city.OperationOrdinal,
                state.AbyssFloors,towerReceipts,state.AboveGroundChangeIds,
                Array.Empty<AbyssAuthorityEntry022>(),root,migrationReceipt,
                null,Array.Empty<AbyssStepReceiptProof022>(),
                state.LegacyAbyssOperationArchives);
            return Success(campaign,city,strategic,progress,playable,migrated);
        }

        public Result<CampaignState> CommitAbyssStep(CampaignState campaign,ICampaignRegistry022 registry,string outcome)
        {
            if(!TryContext(campaign,out var city,out var strategic,out var progress,out var playable,out var state,out var error)||registry==null)return Result<CampaignState>.Failure(error??"CAMPAIGN022_INPUT_REQUIRED");var active=state.ActiveAbyssOperation;if(active==null)return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_ACTIVE_REQUIRED");if(!registry.AbyssOperations.TryGetValue(active.OperationDefinitionId,out var op))return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_OPERATION_UNKNOWN");var lawError=ValidateAbyssOperationLaw(registry,op,out _);if(lawError!=null)return Result<CampaignState>.Failure(lawError);if(!HasCanonicalActiveAbyssIdentity(campaign,registry,active))return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_ACTIVE_AUTHORITY_INVALID");if(active.CurrentStepIndex>=op.steps.Length)return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_STEP_RANGE");var step=op.steps[active.CurrentStepIndex];if(step.requiresBattle)return Result<CampaignState>.Failure("CAMPAIGN022_CERTIFIED_BATTLE_REQUIRED");var expected=CreateAbyssNonBattleStepReceipt(active,step,outcome??"SUCCESS");if(active.PendingReceipt!=null)return ReceiptsMatch(active.PendingReceipt,expected)?Result<CampaignState>.Success(campaign):Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_STEP_RECEIPT_INVALID");active=active.With(pendingReceipt:expected,replacePendingReceipt:true);return Success(campaign,city,strategic,progress,playable,state.With(activeAbyssOperation:active,replaceActiveAbyssOperation:true,lastCheckpointId:"campaign022_abyss_step_committed"));
        }

        public Result<CampaignState> ApplyAbyssStep(CampaignState campaign,ICampaignRegistry022 registry)
        {
            if((campaign?.Guild?.Development?.AppliedAdventureAuthorityIds.Count??0)>=
               GuildDevelopmentState.AdventureAuthorityEntryLimit)
                return Result<CampaignState>.Failure(
                    "CAMPAIGN022_ADVENTURE_AUTHORITY_LEDGER_FULL");
            if(!TryContext(campaign,out var city,out var strategic,out var progress,out var playable,out var state,out var error)||registry==null)return Result<CampaignState>.Failure(error??"CAMPAIGN022_INPUT_REQUIRED");var active=state.ActiveAbyssOperation;if(active?.PendingReceipt==null)return Result<CampaignState>.Failure("CAMPAIGN022_PENDING_RECEIPT_REQUIRED");if(!registry.AbyssOperations.TryGetValue(active.OperationDefinitionId,out var op))return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_OPERATION_UNKNOWN");var lawError=ValidateAbyssOperationLaw(registry,op,out _);if(lawError!=null)return Result<CampaignState>.Failure(lawError);if(!HasCanonicalActiveAbyssIdentity(campaign,registry,active))return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_ACTIVE_AUTHORITY_INVALID");if(active.CurrentStepIndex>=op.steps.Length)return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_STEP_RANGE");var step=op.steps[active.CurrentStepIndex];if(step.requiresBattle)return Result<CampaignState>.Failure("CAMPAIGN022_CERTIFIED_BATTLE_REQUIRED");var expected=CreateAbyssNonBattleStepReceipt(active,step,active.PendingReceipt.Outcome);if(!ReceiptsMatch(active.PendingReceipt,expected))return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_STEP_RECEIPT_INVALID");if(state.AppliedReceiptIds.Contains(expected.ReceiptId)||campaign.Guild.Development.HasAdventureAuthority(expected.ReceiptId))return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_RECEIPT_STATE_INCONSISTENT");var complete=new List<string>(active.CompletedStepIds){step.stepId};var applied=new List<string>(state.AppliedReceiptIds){expected.ReceiptId};var proofs=new List<AbyssStepReceiptProof022>(active.AppliedStepProofs){new AbyssStepReceiptProof022(step.stepId,false,expected.Outcome,expected,string.Empty,string.Empty,string.Empty,string.Empty,string.Empty,string.Empty)};var nextIndex=active.CurrentStepIndex+1;var nextStatus=nextIndex>=op.steps.Length?AbyssOperationStatus022.ReadyToFinalize:AbyssOperationStatus022.Active;active=active.With(currentStepIndex:nextIndex,status:nextStatus,completedStepIds:complete.AsReadOnly(),pendingReceipt:null,replacePendingReceipt:true,appliedStepProofs:proofs.AsReadOnly());var development=campaign.Guild.Development.RecordAdventureAuthority(expected.ReceiptId);campaign=campaign.With(campaign.Guild.With(campaign.Guild.TreasuryXp,campaign.Guild.Recruits,campaign.Guild.Unions,campaign.Guild.Inventory,development),campaign.OpeningFlow);return Success(campaign,city,strategic,progress,playable,state.With(activeAbyssOperation:active,replaceActiveAbyssOperation:true,appliedReceiptIds:applied.AsReadOnly(),lastCheckpointId:"campaign022_abyss_step_applied",activeAbyssStepLedger:proofs.AsReadOnly()));
        }

        public Result<CampaignState> CommitAbyssBattleEncounter(CampaignState campaign,ICampaignRegistry022 registry)
        {
            if(!TryContext(campaign,out var city,out var strategic,out var progress,out var playable,out var state,out var error)||registry==null)return Result<CampaignState>.Failure(error??"CAMPAIGN022_INPUT_REQUIRED");var active=state.ActiveAbyssOperation;
            if(!TryGetActiveAbyssBattleStep(active,registry,out var op,out var step,out error))return Result<CampaignState>.Failure(error);
            if(!HasCanonicalActiveAbyssIdentity(campaign,registry,active))
                return Result<CampaignState>.Failure(
                    "CAMPAIGN022_ABYSS_ACTIVE_AUTHORITY_INVALID");
            if(active.PendingReceipt!=null)return Result<CampaignState>.Failure("CAMPAIGN022_APPLY_PENDING_ABYSS_RECEIPT_FIRST");
            if(city.PendingBattleReturn!=null)return Result<CampaignState>.Failure("CAMPAIGN022_APPLY_EXISTING_BATTLE_RETURN_FIRST");
            var expected=CreateAbyssBattleEncounterRequest(campaign,active,op,step);
            if(expected.AlliedUnionIds.Count==0)return Result<CampaignState>.Failure("CAMPAIGN022_ACTIVE_UNION_REQUIRED");
            if(city.PendingEncounter!=null)
            {
                if(!AbyssEncountersMatch(city.PendingEncounter,expected)||
                   !campaign.Guild.Development.HasAdventureAuthority(
                       GuildCityBattleBridgeService017D
                           .EncounterRequestAuthorityId084(expected)))return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_ENCOUNTER_INVALID");
                if(active.Status!=AbyssOperationStatus022.AwaitingBattle)return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_ENCOUNTER_STATE_INCONSISTENT");
                if(campaign.Battle!=null&&campaign.Battle.Outcome==BattleOutcome.InProgress&&!StringComparer.Ordinal.Equals(campaign.Battle.BattleId,expected.BattleId))return Result<CampaignState>.Failure("CAMPAIGN022_ACTIVE_BATTLE_MUST_FINISH");
                if(campaign.Battle!=null&&campaign.Battle.Phase==BattlePhase.Resolved&&campaign.Battle.Reward!=null&&!campaign.Battle.Reward.Claimed)return Result<CampaignState>.Failure("CAMPAIGN022_CLAIM_EXISTING_REWARD_FIRST");
                if(campaign.Battle!=null&&campaign.Battle.Phase==BattlePhase.Resolved&&campaign.Battle.Reward?.Claimed==true&&StringComparer.Ordinal.Equals(campaign.Battle.BattleId,expected.BattleId))return Result<CampaignState>.Failure("CAMPAIGN022_RETURN_CLAIMED_ABYSS_BATTLE_FIRST");
                return Result<CampaignState>.Success(campaign);
            }
            if(campaign.Battle!=null&&campaign.Battle.Outcome==BattleOutcome.InProgress)return Result<CampaignState>.Failure("CAMPAIGN022_ACTIVE_BATTLE_MUST_FINISH");
            if(campaign.Battle!=null&&campaign.Battle.Phase==BattlePhase.Resolved&&campaign.Battle.Reward!=null&&!campaign.Battle.Reward.Claimed)return Result<CampaignState>.Failure("CAMPAIGN022_CLAIM_EXISTING_REWARD_FIRST");
            if(active.Status!=AbyssOperationStatus022.Active)return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_BATTLE_ENTRY_STATE_INVALID");
            var requestAuthority=GuildCityBattleBridgeService017D
                .EncounterRequestAuthorityId084(expected);
            var development=campaign.Guild.Development;
            if(development.HasAdventureAuthority(requestAuthority))
                return Result<CampaignState>.Failure(
                    "CAMPAIGN022_ENCOUNTER_AUTHORITY_ALREADY_COMMITTED");
            if(!development.CanRecordAdventureAuthority(requestAuthority))
                return Result<CampaignState>.Failure(
                    "CAMPAIGN022_ADVENTURE_AUTHORITY_LEDGER_FULL");
            development=development.RecordAdventureAuthority(requestAuthority);
            campaign=campaign.With(campaign.Guild.With(campaign.Guild.TreasuryXp,
                campaign.Guild.Recruits,campaign.Guild.Unions,
                campaign.Guild.Inventory,development),campaign.OpeningFlow);
            active=active.With(status:AbyssOperationStatus022.AwaitingBattle);var next=state.With(activeAbyssOperation:active,replaceActiveAbyssOperation:true,lastCheckpointId:"campaign022_abyss_battle_committed");city=city.With(pendingEncounter:expected,replacePendingEncounter:true,lastCheckpointId:"campaign022_abyss_battle_committed");return Success(campaign,city,strategic,progress,playable,next);
        }

        public Result<CampaignState> MarkAbyssBattleCommitted(CampaignState campaign,ICampaignRegistry022 registry)=>CommitAbyssBattleEncounter(campaign,registry);

        public bool HasActiveAbyssEncounter(CampaignState campaign,ICampaignRegistry022 registry)
        {
            if(registry==null||!TryContext(campaign,out var city,out _,out _,out _,out var state,out _))return false;var active=state.ActiveAbyssOperation;if(active==null||active.Status!=AbyssOperationStatus022.AwaitingBattle||active.PendingReceipt!=null||city.PendingEncounter==null||!HasCanonicalActiveAbyssIdentity(campaign,registry,active))return false;if(!TryGetActiveAbyssBattleStep(active,registry,out var op,out var step,out _))return false;var expected=CreateAbyssBattleEncounterRequest(campaign,active,op,step);return AbyssEncountersMatch(city.PendingEncounter,expected)&&campaign.Guild.Development.HasAdventureAuthority(GuildCityBattleBridgeService017D.EncounterRequestAuthorityId084(expected));
        }

        public bool HasMatchingActiveAbyssBattle(CampaignState campaign,ICampaignRegistry022 registry)
        {
            if(registry==null||campaign?.Battle==null||
               !TryContext(campaign,out var city,out _,out _,out _,out var state,out _))return false;
            var active=state.ActiveAbyssOperation;
            if(active==null||active.Status!=AbyssOperationStatus022.AwaitingBattle||
               !HasCanonicalActiveAbyssIdentity(campaign,registry,active)||
               !TryGetActiveAbyssBattleStep(active,registry,out var op,out var step,out _))return false;
            var expected=CreateAbyssBattleEncounterRequest(campaign,active,op,step);
            if(!StringComparer.Ordinal.Equals(campaign.Battle.BattleId,expected.BattleId))return false;
            if(city.PendingEncounter!=null)return
                StringComparer.Ordinal.Equals(city.PendingEncounter.ExpeditionId,active.OperationInstanceId)&&
                AbyssEncountersMatch(city.PendingEncounter,expected);
            if(campaign.Battle.Phase!=BattlePhase.Resolved||campaign.Battle.Reward?.Claimed!=true)return false;
            var appliedReturn=CreateAbyssBattleReturnReceipt(campaign,active,step,expected,out _);
            return appliedReturn!=null&&city.AppliedBattleReturnIds.Contains(appliedReturn.ReceiptId);
        }

        public Result<CampaignState> CommitAbyssBattleReturn(CampaignState campaign,ICampaignRegistry022 registry)
        {
            if(!TryContext(campaign,out var city,out var strategic,out var progress,out var playable,out var state,out var error)||registry==null)return Result<CampaignState>.Failure(error??"CAMPAIGN022_INPUT_REQUIRED");var active=state.ActiveAbyssOperation;
            if(active==null||active.Status!=AbyssOperationStatus022.AwaitingBattle)return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_AWAITING_BATTLE_REQUIRED");if(!TryGetActiveAbyssBattleStep(active,registry,out var op,out var step,out error))return Result<CampaignState>.Failure(error);
            if(!HasCanonicalActiveAbyssIdentity(campaign,registry,active))
                return Result<CampaignState>.Failure(
                    "CAMPAIGN022_ABYSS_ACTIVE_AUTHORITY_INVALID");
            var request=CreateAbyssBattleEncounterRequest(campaign,active,op,step);if(city.PendingEncounter==null||!AbyssEncountersMatch(city.PendingEncounter,request))return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_ENCOUNTER_INVALID");
            var receipt=CreateAbyssBattleReturnReceipt(campaign,active,step,request,out error);if(receipt==null)return Result<CampaignState>.Failure(error);
            if(city.PendingBattleReturn!=null)return AbyssBattleReturnsMatch(city.PendingBattleReturn,receipt)?Result<CampaignState>.Success(campaign):Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_BATTLE_RETURN_INVALID");
            city=city.With(pendingBattleReturn:receipt,replacePendingBattleReturn:true,lastCheckpointId:"campaign022_abyss_battle_return_committed");return Success(campaign,city,strategic,progress,playable,state.With(lastCheckpointId:"campaign022_abyss_battle_return_committed"));
        }

        public Result<CampaignState> ApplyAbyssBattleReturnExactlyOnce(CampaignState campaign,ICampaignRegistry022 registry)
        {
            if(!TryContext(campaign,out var city,out var strategic,out var progress,out var playable,out var state,out var error)||registry==null)return Result<CampaignState>.Failure(error??"CAMPAIGN022_INPUT_REQUIRED");var active=state.ActiveAbyssOperation;
            if(active==null||active.Status!=AbyssOperationStatus022.AwaitingBattle)return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_AWAITING_BATTLE_REQUIRED");if(!TryGetActiveAbyssBattleStep(active,registry,out var op,out var step,out error))return Result<CampaignState>.Failure(error);
            if(!HasCanonicalActiveAbyssIdentity(campaign,registry,active))
                return Result<CampaignState>.Failure(
                    "CAMPAIGN022_ABYSS_ACTIVE_AUTHORITY_INVALID");
            var request=CreateAbyssBattleEncounterRequest(campaign,active,op,step);var expected=CreateAbyssBattleReturnReceipt(campaign,active,step,request,out error);if(expected==null)return Result<CampaignState>.Failure(error);var returnAuthority=GuildCityBattleBridgeService017D.BattleReturnApplyAuthorityId084(expected);
            if(city.PendingBattleReturn==null)
            {
                if(city.PendingEncounter==null&&city.AppliedBattleReturnIds.Contains(expected.ReceiptId)&&campaign.Guild.Development.HasAdventureAuthority(returnAuthority))return Result<CampaignState>.Success(campaign);
                return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_BATTLE_RETURN_REQUIRED");
            }
            if(city.PendingEncounter==null||!AbyssEncountersMatch(city.PendingEncounter,request)||!AbyssBattleReturnsMatch(city.PendingBattleReturn,expected))return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_BATTLE_RETURN_INVALID");
            if(city.AppliedBattleReturnIds.Contains(expected.ReceiptId)||campaign.Guild.Development.HasAdventureAuthority(returnAuthority))return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_BATTLE_RETURN_STATE_INCONSISTENT");
            if(!campaign.Guild.Development.CanRecordAdventureAuthority(returnAuthority))return Result<CampaignState>.Failure("CAMPAIGN022_ADVENTURE_AUTHORITY_LEDGER_FULL");
            var development=campaign.Guild.Development.RecordAdventureAuthority(returnAuthority);campaign=campaign.With(campaign.Guild.With(campaign.Guild.TreasuryXp,campaign.Guild.Recruits,campaign.Guild.Unions,campaign.Guild.Inventory,development),campaign.OpeningFlow);
            var applied=new List<string>(city.AppliedBattleReturnIds){expected.ReceiptId};city=city.With(pendingEncounter:null,replacePendingEncounter:true,pendingBattleReturn:null,replacePendingBattleReturn:true,appliedBattleReturnIds:applied.AsReadOnly(),lastCheckpointId:"campaign022_abyss_battle_return_applied");return Success(campaign,city,strategic,progress,playable,state.With(lastCheckpointId:"campaign022_abyss_battle_return_applied:"+expected.ReceiptId));
        }

        public Result<CampaignState> CommitAbyssBattleResult(CampaignState campaign,ICampaignRegistry022 registry)
        {
            if(!TryContext(campaign,out var city,out var strategic,out var progress,out var playable,out var state,out var error)||registry==null)return Result<CampaignState>.Failure(error??"CAMPAIGN022_INPUT_REQUIRED");
            var active=state.ActiveAbyssOperation;
            if(active==null||active.Status!=AbyssOperationStatus022.AwaitingBattle)return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_AWAITING_BATTLE_REQUIRED");
            if(!HasCanonicalActiveAbyssIdentity(campaign,registry,active))
                return Result<CampaignState>.Failure(
                    "CAMPAIGN022_ABYSS_ACTIVE_AUTHORITY_INVALID");
            if(!registry.AbyssOperations.TryGetValue(active.OperationDefinitionId,out var op))return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_OPERATION_UNKNOWN");
            if(active.CurrentStepIndex>=op.steps.Length||!op.steps[active.CurrentStepIndex].requiresBattle)return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_BATTLE_STEP_REQUIRED");
            if(city.PendingEncounter!=null||city.PendingBattleReturn!=null)return Result<CampaignState>.Failure("CAMPAIGN022_APPLY_EXISTING_BATTLE_RETURN_FIRST");
            if(campaign.Battle==null||campaign.Battle.Phase!=BattlePhase.Resolved||campaign.Battle.Reward==null||!campaign.Battle.Reward.Claimed)return Result<CampaignState>.Failure("CAMPAIGN022_CLAIM_EXISTING_REWARD_FIRST");
            if(campaign.Battle.Outcome!=BattleOutcome.Victory||campaign.Battle.Reward.Outcome!=BattleOutcome.Victory)return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_BATTLE_VICTORY_REQUIRED");
            var step=op.steps[active.CurrentStepIndex];var request=CreateAbyssBattleEncounterRequest(campaign,active,op,step);var returnReceipt=CreateAbyssBattleReturnReceipt(campaign,active,step,request,out error);if(returnReceipt==null||!city.AppliedBattleReturnIds.Contains(returnReceipt.ReceiptId))return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_BATTLE_RETURN_REQUIRED");var returnAuthority=GuildCityBattleBridgeService017D.BattleReturnApplyAuthorityId084(returnReceipt);if(!campaign.Guild.Development.HasAdventureAuthority(returnAuthority))return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_BATTLE_RETURN_AUTHORITY_REQUIRED");
            var receipt=CreateAbyssBattleReceipt(active,op,step,campaign.Battle,returnAuthority);
            if(active.PendingReceipt!=null)
            {
                if(!ReceiptsMatch(active.PendingReceipt,receipt)||!StringComparer.Ordinal.Equals(active.ExistingBattleRewardReceiptId,campaign.Battle.Reward.RewardId))return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_BATTLE_RECEIPT_INVALID");
                return Result<CampaignState>.Success(campaign);
            }
            active=active.With(pendingReceipt:receipt,replacePendingReceipt:true,existingBattleRewardReceiptId:campaign.Battle.Reward.RewardId);
            return Success(campaign,city,strategic,progress,playable,state.With(activeAbyssOperation:active,replaceActiveAbyssOperation:true,lastCheckpointId:"campaign022_abyss_battle_result"));
        }

        public Result<CampaignState> CommitAbyssOperationCompletion(CampaignState campaign,ICampaignRegistry022 registry)
        {
            if(!TryContext(campaign,out var city,out var strategic,out var progress,out var playable,out var state,out var error)||registry==null)return Result<CampaignState>.Failure(error??"CAMPAIGN022_INPUT_REQUIRED");
            var active=state.ActiveAbyssOperation;
            if(active==null||active.Status!=AbyssOperationStatus022.ReadyToFinalize)return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_READY_TO_FINALIZE_REQUIRED");
            if(!HasCanonicalActiveAbyssIdentity(campaign,registry,active))
                return Result<CampaignState>.Failure(
                    "CAMPAIGN022_ABYSS_ACTIVE_AUTHORITY_INVALID");
            if(!registry.AbyssOperations.TryGetValue(active.OperationDefinitionId,out var op))return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_OPERATION_UNKNOWN");
            var lawError=ValidateAbyssOperationLaw(registry,op,out _);if(lawError!=null)return Result<CampaignState>.Failure(lawError);
            if(!HasCompletedEveryAbyssStep(active,op))return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_COMPLETION_STEPS_REQUIRED");
            if(!HasRequiredAbyssBattleAuthority(campaign,state,active,op))return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_BATTLE_AUTHORITY_REQUIRED");
            if(!HasCanonicalCompletedAbyssStepProof(campaign,city,state,active,op))return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_STEP_PROOF_INVALID");
            var receipt=CreateAbyssOperationCompletionReceipt(active,op);
            if(active.PendingReceipt!=null)
            {
                if(!ReceiptsMatch(active.PendingReceipt,receipt))return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_COMPLETION_RECEIPT_INVALID");
                return Result<CampaignState>.Success(campaign);
            }
            active=active.With(pendingReceipt:receipt,replacePendingReceipt:true);
            return Success(campaign,city,strategic,progress,playable,state.With(activeAbyssOperation:active,replaceActiveAbyssOperation:true,lastCheckpointId:"campaign022_abyss_completion_committed"));
        }

        public Result<CampaignState> ApplyAbyssOperationCompletion(CampaignState campaign,ICampaignRegistry022 registry, GuildCityRecruitmentService017D towerRecruitment094 = null)
        {
            if(!TryContext(campaign,out var city,out var strategic,out var progress,out var playable,out var state,out var error)||registry==null)return Result<CampaignState>.Failure(error??"CAMPAIGN022_INPUT_REQUIRED");
            var active=state.ActiveAbyssOperation;
            if(active==null||active.Status!=AbyssOperationStatus022.ReadyToFinalize)return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_READY_TO_FINALIZE_REQUIRED");
            if(!HasCanonicalActiveAbyssIdentity(campaign,registry,active))
                return Result<CampaignState>.Failure(
                    "CAMPAIGN022_ABYSS_ACTIVE_AUTHORITY_INVALID");
            if(!registry.AbyssOperations.TryGetValue(active.OperationDefinitionId,out var op))return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_OPERATION_UNKNOWN");
            var lawError=ValidateAbyssOperationLaw(registry,op,out _);if(lawError!=null)return Result<CampaignState>.Failure(lawError);
            if(!HasCompletedEveryAbyssStep(active,op))return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_COMPLETION_STEPS_REQUIRED");
            if(!HasRequiredAbyssBattleAuthority(campaign,state,active,op))return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_BATTLE_AUTHORITY_REQUIRED");
            if(!HasCanonicalCompletedAbyssStepProof(campaign,city,state,active,op))return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_STEP_PROOF_INVALID");
            if(active.PendingReceipt==null)return Result<CampaignState>.Failure("CAMPAIGN022_PENDING_RECEIPT_REQUIRED");
            var expected=CreateAbyssOperationCompletionReceipt(active,op);
            if(!ReceiptsMatch(active.PendingReceipt,expected))return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_COMPLETION_RECEIPT_INVALID");
            return ApplyAbyssCompletionReceipt(campaign,city,strategic,progress,playable,state,registry,active.PendingReceipt,towerRecruitment094);
        }

        public Result<CampaignState> ApplyAbyssBattleAndFinalize(CampaignState campaign,ICampaignRegistry022 registry)
        {
            if((campaign?.Guild?.Development?.AppliedAdventureAuthorityIds.Count??0)>=
               GuildDevelopmentState.AdventureAuthorityEntryLimit)
                return Result<CampaignState>.Failure(
                    "CAMPAIGN022_ADVENTURE_AUTHORITY_LEDGER_FULL");
            if(!TryContext(campaign,out var city,out var strategic,out var progress,out var playable,out var state,out var error)||registry==null)return Result<CampaignState>.Failure(error??"CAMPAIGN022_INPUT_REQUIRED");
            var active=state.ActiveAbyssOperation;
            if(active==null||active.Status!=AbyssOperationStatus022.AwaitingBattle)return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_AWAITING_BATTLE_REQUIRED");
            if(!HasCanonicalActiveAbyssIdentity(campaign,registry,active))
                return Result<CampaignState>.Failure(
                    "CAMPAIGN022_ABYSS_ACTIVE_AUTHORITY_INVALID");
            if(!registry.AbyssOperations.TryGetValue(active.OperationDefinitionId,out var op))return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_OPERATION_UNKNOWN");
            var lawError=ValidateAbyssOperationLaw(registry,op,out _);if(lawError!=null)return Result<CampaignState>.Failure(lawError);
            if(active.CurrentStepIndex>=op.steps.Length||!op.steps[active.CurrentStepIndex].requiresBattle)return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_BATTLE_STEP_REQUIRED");
            if(active.PendingReceipt==null)return Result<CampaignState>.Failure("CAMPAIGN022_PENDING_RECEIPT_REQUIRED");
            if(!active.PendingReceipt.ReceiptId.StartsWith("ABYSSREC022_",StringComparison.Ordinal)||!StringComparer.Ordinal.Equals(active.PendingReceipt.SourceId,active.OperationDefinitionId)||!StringComparer.Ordinal.Equals(active.PendingReceipt.Outcome,"VICTORY")||active.PendingReceipt.AppliedVersion!=0)return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_BATTLE_RECEIPT_INVALID");
            if(city.PendingEncounter!=null||city.PendingBattleReturn!=null)return Result<CampaignState>.Failure("CAMPAIGN022_APPLY_EXISTING_BATTLE_RETURN_FIRST");
            if(campaign.Battle==null||campaign.Battle.Phase!=BattlePhase.Resolved||campaign.Battle.Reward==null||!campaign.Battle.Reward.Claimed)return Result<CampaignState>.Failure("CAMPAIGN022_CLAIM_EXISTING_REWARD_FIRST");
            if(campaign.Battle.Outcome!=BattleOutcome.Victory||campaign.Battle.Reward.Outcome!=BattleOutcome.Victory)return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_BATTLE_VICTORY_REQUIRED");
            if(string.IsNullOrWhiteSpace(active.ExistingBattleRewardReceiptId)||!StringComparer.Ordinal.Equals(active.ExistingBattleRewardReceiptId,campaign.Battle.Reward.RewardId)||!campaign.Guild.Development.HasClaimedReward(active.ExistingBattleRewardReceiptId))return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_EXISTING_REWARD_LINK_REQUIRED");
            var step=op.steps[active.CurrentStepIndex];
            var request=CreateAbyssBattleEncounterRequest(campaign,active,op,step);var returnReceipt=CreateAbyssBattleReturnReceipt(campaign,active,step,request,out error);if(returnReceipt==null||!city.AppliedBattleReturnIds.Contains(returnReceipt.ReceiptId))return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_BATTLE_RETURN_REQUIRED");var returnAuthority=GuildCityBattleBridgeService017D.BattleReturnApplyAuthorityId084(returnReceipt);if(!campaign.Guild.Development.HasAdventureAuthority(returnAuthority))return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_BATTLE_RETURN_AUTHORITY_REQUIRED");
            var expected=CreateAbyssBattleReceipt(active,op,step,campaign.Battle,returnAuthority);
            if(!ReceiptsMatch(active.PendingReceipt,expected))return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_BATTLE_RECEIPT_INVALID");
            if(state.AppliedReceiptIds.Contains(active.PendingReceipt.ReceiptId)||
               campaign.Guild.Development.HasAdventureAuthority(
                   active.PendingReceipt.ReceiptId))return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_RECEIPT_STATE_INCONSISTENT");
            var completed=new List<string>(active.CompletedStepIds){step.stepId};
            var applied=new List<string>(state.AppliedReceiptIds){active.PendingReceipt.ReceiptId};
            var proofs=new List<AbyssStepReceiptProof022>(active.AppliedStepProofs){new AbyssStepReceiptProof022(step.stepId,true,"VICTORY",expected,campaign.Battle.FinalStateHash,campaign.Battle.Reward.RewardId,returnReceipt.ReceiptId,request.RequestId,campaign.Battle.BattleId,request.ReturnCheckpointId,returnAuthority)};
            var nextIndex=active.CurrentStepIndex+1;
            var nextStatus=nextIndex>=op.steps.Length?AbyssOperationStatus022.ReadyToFinalize:AbyssOperationStatus022.Active;
            active=active.With(currentStepIndex:nextIndex,status:nextStatus,completedStepIds:completed.AsReadOnly(),pendingReceipt:null,replacePendingReceipt:true,appliedStepProofs:proofs.AsReadOnly());
            var development=campaign.Guild.Development.RecordAdventureAuthority(
                expected.ReceiptId);campaign=campaign.With(campaign.Guild.With(
                    campaign.Guild.TreasuryXp,campaign.Guild.Recruits,
                    campaign.Guild.Unions,campaign.Guild.Inventory,development),
                campaign.OpeningFlow);
            return Success(campaign,city,strategic,progress,playable,state.With(activeAbyssOperation:active,replaceActiveAbyssOperation:true,appliedReceiptIds:applied.AsReadOnly(),lastCheckpointId:"campaign022_abyss_battle_step_applied",activeAbyssStepLedger:proofs.AsReadOnly()));
        }

        public Result<CampaignState> GrantSpecialInvocationRelic(
            CampaignState campaign,
            ICampaignRegistry022 registry,
            ISpecialRelicCatalog001 relicCatalog,
            string relicId,
            string sourceReceiptId)
        {
            if(!TryContext(campaign,out var city,out var strategic,out var progress,out var playable,out var state,out var error)||registry==null||relicCatalog==null)
                return Result<CampaignState>.Failure(error??"SPECIAL_RELIC001_INPUT_REQUIRED");
            if(string.IsNullOrWhiteSpace(sourceReceiptId))
                return Result<CampaignState>.Failure("SPECIAL_RELIC001_SOURCE_RECEIPT_REQUIRED");
            if(!SpecialRelicInvocationRules001.TryGet(relicId,out var localRule)||
               !relicCatalog.IsP0(relicId)||
               !relicCatalog.TryGetRelic(relicId,out var catalogRule)||
               catalogRule==null||
               !StringComparer.Ordinal.Equals(catalogRule.Kind,"INVOCATION")||
               !StringComparer.OrdinalIgnoreCase.Equals(catalogRule.Rarity,"Rare")||
               !catalogRule.IsPublicCodeEligible||
               !StringComparer.Ordinal.Equals(catalogRule.Name,localRule.DisplayName)||
               !StringComparer.Ordinal.Equals(catalogRule.Summon,localRule.SummonName))
                return Result<CampaignState>.Failure("SPECIAL_RELIC001_P0_INVOCATION_REQUIRED");
            if(!InvocationArtifactAuthority022.TryResolveBaseAndPath(registry,localRule.CanonicalBaseId,out var definition,out var path,out error))
                return Result<CampaignState>.Failure(error);
            if(!registry.WeaponTracks.TryGetValue(localRule.EquipmentTrackId,out var track)||track==null||
               !StringComparer.Ordinal.Equals(track.weaponFamilyId,definition.weaponFamilyId))
                return Result<CampaignState>.Failure("SPECIAL_RELIC001_EQUIPMENT_TRACK_INVALID");

            var grantHash=CanonicalJson.Sha256Hex(new
            {
                campaign.CampaignGuid,
                relicId,
                sourceReceiptId,
                Authority=SpecialRelicInvocationRules001.LocalBalanceAuthority
            });
            var grantReceiptId="SPRELGRANT001_"+grantHash.Substring(0,24).ToUpperInvariant();
            var instanceHash=CanonicalJson.Sha256Hex(new
            {
                campaign.CampaignGuid,
                relicId,
                UniqueOwnedRelic=true,
                Authority=SpecialRelicInvocationRules001.LocalBalanceAuthority
            });
            var instanceId="SPECIALRELIC001_"+instanceHash.Substring(0,24).ToUpperInvariant();
            var owned=FindOwnedItemsByDefinition(campaign.Guild,relicId);
            if(owned.Count>1)return Result<CampaignState>.Failure("SPECIAL_RELIC001_INVOCATION_ITEM_AMBIGUOUS");
            if(owned.Count==1)
            {
                var existing=owned[0];
                var matchingArtifacts=state.InvocationArtifacts.Where(x=>x!=null&&StringComparer.Ordinal.Equals(x.InstanceId,existing.InstanceId)).ToArray();
                if(!StringComparer.Ordinal.Equals(existing.InstanceId,instanceId)||matchingArtifacts.Length!=1||
                   !InvocationArtifactAuthority022.TryValidateArtifact(registry,matchingArtifacts[0],existing,out _,out _,out error))
                    return Result<CampaignState>.Failure(error??"SPECIAL_RELIC001_INVOCATION_STATE_INVALID");
                if(state.AppliedReceiptIds.Contains(grantReceiptId))return Result<CampaignState>.Success(campaign);
                var existingReceipts=new List<string>(state.AppliedReceiptIds){grantReceiptId};
                existingReceipts.Sort(StringComparer.Ordinal);
                return Success(campaign,city,strategic,progress,playable,state.With(
                    appliedReceiptIds:existingReceipts.AsReadOnly(),
                    lastCheckpointId:"special_relic001_invocation_grant:"+relicId));
            }
            if(state.AppliedReceiptIds.Contains(grantReceiptId))
                return Result<CampaignState>.Failure("SPECIAL_RELIC001_GRANT_RECEIPT_STATE_INCONSISTENT");
            var collidingOwnedItem=FindUniqueOwnedEquipmentItem(campaign.Guild,instanceId,out var collisionError);
            if(collidingOwnedItem!=null||
               !StringComparer.Ordinal.Equals(collisionError,"CAMPAIGN022_INVOCATION_ITEM_NOT_OWNED")||
               state.InvocationArtifacts.Any(x=>x!=null&&StringComparer.Ordinal.Equals(x.InstanceId,instanceId)))
                return Result<CampaignState>.Failure("SPECIAL_RELIC001_INSTANCE_ID_COLLISION");

            var artifact=new InvocationArtifactState022(instanceId,localRule.CanonicalBaseId,Array.Empty<string>(),path.pathId,0,0,false);
            var item=new EquipmentItemState(
                instanceId,relicId,localRule.ActiveDisplayName,new[]{EquipmentSlotIds.ToolRelic},
                new[]{InvocationArtifactAuthority022.InvocationEquipmentTag,definition.weaponFamilyId,
                    SpecialRelicInvocationRules001.SpecialRelicEquipmentTag,
                    SpecialRelicInvocationRules001.ManualEquipOnlyTag},
                SpecialRelicInvocationRules001.QualityId,10000,false);
            if(!InvocationArtifactAuthority022.TryValidateArtifact(registry,artifact,item,out _,out _,out error))
                return Result<CampaignState>.Failure(error);
            var artifacts=new List<InvocationArtifactState022>(state.InvocationArtifacts){artifact};
            var inventory=new List<EquipmentItemState>(campaign.Guild.Inventory){item};
            inventory.Sort((a,b)=>StringComparer.Ordinal.Compare(a.InstanceId,b.InstanceId));
            var receipts=new List<string>(state.AppliedReceiptIds){grantReceiptId};
            receipts.Sort(StringComparer.Ordinal);
            var guild=campaign.Guild.With(campaign.Guild.TreasuryXp,campaign.Guild.Recruits,campaign.Guild.Unions,inventory.AsReadOnly(),campaign.Guild.Development);
            var next=state.With(
                invocationArtifacts:artifacts.AsReadOnly(),
                appliedReceiptIds:receipts.AsReadOnly(),
                lastCheckpointId:"special_relic001_invocation_grant:"+relicId);
            return Success(
                campaign.With(guild,campaign.OpeningFlow),city,strategic,progress,
                playable.With(progression022:next,replaceProgression022:true,lastCheckpointId:next.LastCheckpointId),next);
        }

        public Result<CampaignState> CraftInvocationArtifact(CampaignState campaign,ICampaignRegistry022 registry,string baseId,IReadOnlyList<string> affixIds)
        {
            if(!TryContext(campaign,out var city,out var strategic,out var progress,out var playable,out var state,out var error)||registry==null)return Result<CampaignState>.Failure(error??"CAMPAIGN022_INPUT_REQUIRED");
            if(!InvocationArtifactAuthority022.TryResolveBaseAndPath(registry,baseId,out var definition,out var path,out error))return Result<CampaignState>.Failure(error);
            if(!InvocationArtifactAuthority022.TryCanonicalizeAffixIds(registry,affixIds,out var affixes,out error))return Result<CampaignState>.Failure(error);
            if(!TrySpend(playable.WorldMaterials,definition.materialCosts,out var materials))return Result<CampaignState>.Failure("CAMPAIGN022_MATERIALS_INSUFFICIENT");
            var hash=CanonicalJson.Sha256Hex(new{campaign.CampaignGuid,baseId,affixes,count=state.InvocationArtifacts.Count});
            var instanceId="INVOCATION022_"+hash.Substring(0,24).ToUpperInvariant();
            if(state.InvocationArtifacts.Any(x=>StringComparer.Ordinal.Equals(x.InstanceId,instanceId)))return Result<CampaignState>.Success(campaign);
            var artifact=new InvocationArtifactState022(instanceId,baseId,affixes,path.pathId,0,0,false);
            var item=new EquipmentItemState(instanceId,baseId,definition.displayName,new[]{definition.validSlotId},new[]{InvocationArtifactAuthority022.InvocationEquipmentTag,definition.weaponFamilyId},"UNCOMMON",10000,false);
            if(!InvocationArtifactAuthority022.TryValidateArtifact(registry,artifact,item,out _,out _,out error))return Result<CampaignState>.Failure(error);
            var artifacts=new List<InvocationArtifactState022>(state.InvocationArtifacts){artifact};
            var inventory=new List<EquipmentItemState>(campaign.Guild.Inventory){item};inventory.Sort((a,b)=>StringComparer.Ordinal.Compare(a.InstanceId,b.InstanceId));
            var guild=campaign.Guild.With(campaign.Guild.TreasuryXp,campaign.Guild.Recruits,campaign.Guild.Unions,inventory.AsReadOnly(),campaign.Guild.Development);
            var next=state.With(invocationArtifacts:artifacts.AsReadOnly(),lastCheckpointId:"campaign022_artifact_crafted");
            return Success(campaign.With(guild,campaign.OpeningFlow),city,strategic,progress,playable.With(worldMaterials:materials,progression022:next,replaceProgression022:true,lastCheckpointId:"campaign022_artifact_crafted"),next);
        }

        public Result<CampaignState> EvolveInvocationArtifact(CampaignState campaign,ICampaignRegistry022 registry,string itemInstanceId)
        {
            if(!TryContext(campaign,out var city,out var strategic,out var progress,out var playable,out var state,out var error)||registry==null)return Result<CampaignState>.Failure(error??"CAMPAIGN022_INPUT_REQUIRED");
            var matchingArtifacts=state.InvocationArtifacts.Where(x=>x!=null&&StringComparer.Ordinal.Equals(x.InstanceId,itemInstanceId)).ToArray();
            if(matchingArtifacts.Length!=1)return Result<CampaignState>.Failure(matchingArtifacts.Length==0?"CAMPAIGN022_INVOCATION_ARTIFACT_REQUIRED":"CAMPAIGN022_INVOCATION_ARTIFACT_AMBIGUOUS");
            var ownedItem=FindUniqueOwnedEquipmentItem(campaign.Guild,itemInstanceId,out error);
            if(ownedItem==null)return Result<CampaignState>.Failure(error);
            var artifact=matchingArtifacts[0];
            if(!InvocationArtifactAuthority022.TryValidateArtifact(registry,artifact,ownedItem,out _,out var path,out error))return Result<CampaignState>.Failure(error);
            var highestFloor=HighestClearedAbyssFloor(state,registry);
            if(artifact.EvolutionStage>0)
            {
                var currentStage=path.stages.SingleOrDefault(x=>x.stage==artifact.EvolutionStage);
                if(currentStage==null||artifact.Resonance<currentStage.resonanceRequired||highestFloor<currentStage.abyssFloorRequired)return Result<CampaignState>.Failure("CAMPAIGN022_INVOCATION_CURRENT_STAGE_INVALID");
            }
            var nextStage=path.stages.SingleOrDefault(x=>x.stage==artifact.EvolutionStage+1);
            if(nextStage==null)return Result<CampaignState>.Success(campaign);
            if(artifact.Resonance<nextStage.resonanceRequired)return Result<CampaignState>.Failure("CAMPAIGN022_INVOCATION_RESONANCE_REQUIRED");
            if(highestFloor<nextStage.abyssFloorRequired)return Result<CampaignState>.Failure("CAMPAIGN022_INVOCATION_ABYSS_FLOOR_REQUIRED");
            var artifacts=new List<InvocationArtifactState022>(state.InvocationArtifacts);
            var artifactIndex=artifacts.FindIndex(x=>StringComparer.Ordinal.Equals(x.InstanceId,itemInstanceId));
            artifacts[artifactIndex]=artifact.With(evolutionStage:nextStage.stage);
            var next=state.With(invocationArtifacts:artifacts.AsReadOnly(),lastCheckpointId:"campaign022_invocation_artifact_evolved:"+itemInstanceId+":"+nextStage.stage);
            return Success(campaign,city,strategic,progress,playable,next);
        }

        public Result<CampaignState> InvokeEligibleEchoForecast(CampaignState campaign,ICampaignRegistry022 registry,M2BattleCommandService battleCommands,M2CombatContent combatContent)
        {
            if(!TryContext(campaign,out var city,out var strategic,out var progress,out var playable,out var state,out var error)||registry==null||battleCommands==null||combatContent==null)return Result<CampaignState>.Failure(error??"CAMPAIGN022_INPUT_REQUIRED");
            var battle=campaign.Battle;
            if(battle==null||battle.Outcome!=BattleOutcome.InProgress||battle.Phase!=BattlePhase.ForecastSelection)return Result<CampaignState>.Failure("CAMPAIGN022_ECHO_ACTIVE_FORECAST_REQUIRED");
            if(!HasExactlyOneSelectedForecastForEveryActiveUnion(battle))return Result<CampaignState>.Failure("CAMPAIGN022_ECHO_SELECTED_COMPLETE_FORECAST_REQUIRED");
            var highestFloor=HighestClearedAbyssFloor(state,registry);
            EchoInvocationCandidate022 candidate=null;
            var selections=battle.Selections.OrderBy(x=>x.UnionId,StringComparer.Ordinal).ThenBy(x=>x.ForecastId,StringComparer.Ordinal).ToArray();
            for(var selectionIndex=0;selectionIndex<selections.Length&&candidate==null;selectionIndex++)
            {
                var selection=selections[selectionIndex];
                var union=battle.PlayerUnions.FirstOrDefault(x=>StringComparer.Ordinal.Equals(x.UnionId,selection.UnionId));
                var forecast=battle.CommittedForecasts.FirstOrDefault(x=>StringComparer.Ordinal.Equals(x.UnionId,selection.UnionId)&&StringComparer.Ordinal.Equals(x.ForecastId,selection.ForecastId));
                if(union==null||forecast==null||union.IsDefeated||union.Retreated)continue;
                var equipped=FindEquippedInvocationArtifact(campaign,state,registry,union,highestFloor);
                if(equipped==null)continue;
                if(equipped.SpecialRule!=null)
                {
                    var specialEcho=equipped.SpecialRule.CreateEcho();
                    var specialAction=forecast.MemberActions.FirstOrDefault(x=>StringComparer.Ordinal.Equals(x.ActorMemberId,equipped.RecruitId));
                    var specialMember=union.Members.FirstOrDefault(x=>StringComparer.Ordinal.Equals(x.MemberId,equipped.RecruitId));
                    if(EchoCategoryMatchesForecast(specialEcho.forecastCategory,forecast)&&specialAction!=null&&specialMember!=null&&!specialMember.Downed&&
                       forecast.SharedApCost+specialEcho.sharedApCost<=union.CurrentAp&&specialAction.PersonalMpCost+specialEcho.personalMpCost<=specialMember.CurrentMp)
                        candidate=new EchoInvocationCandidate022(specialEcho,equipped.Artifact,union,forecast,specialAction,equipped.SpecialRule);
                    continue;
                }
                if(highestFloor<=0)continue;
                var echoes=registry.Echoes.Values.Where(x=>IsCanonicalEcho(x)&&x.unlockFloor<=highestFloor&&EchoCategoryMatchesForecast(x.forecastCategory,forecast)).OrderByDescending(x=>x.unlockFloor).ThenBy(x=>x.echoId,StringComparer.Ordinal).ToArray();
                for(var echoIndex=0;echoIndex<echoes.Length;echoIndex++)
                {
                    var echo=echoes[echoIndex];
                    var action=forecast.MemberActions.FirstOrDefault(x=>StringComparer.Ordinal.Equals(x.ActorMemberId,equipped.RecruitId));
                    var member=union.Members.FirstOrDefault(x=>StringComparer.Ordinal.Equals(x.MemberId,equipped.RecruitId));
                    if(action==null||member==null||member.Downed||forecast.SharedApCost+echo.sharedApCost>union.CurrentAp||action.PersonalMpCost+echo.personalMpCost>member.CurrentMp)continue;
                    candidate=new EchoInvocationCandidate022(echo,equipped.Artifact,union,forecast,action,null);
                    break;
                }
            }
            if(candidate==null)
            {
                var equippedByUnion=battle.PlayerUnions.Select(x=>FindEquippedInvocationArtifact(campaign,state,registry,x,highestFloor)).Where(x=>x!=null).ToArray();
                var ownsSpecialRelic=SpecialRelicInvocationRules001.All.Any(x=>
                    FindOwnedItemsByDefinition(campaign.Guild,x.RelicId).Count>0);
                if(highestFloor<=0&&!ownsSpecialRelic&&!equippedByUnion.Any(x=>x.SpecialRule!=null))return Result<CampaignState>.Failure("CAMPAIGN022_ECHO_ABYSS_FLOOR_REQUIRED");
                var anyEquipped=equippedByUnion.Length>0;
                if(!anyEquipped)return Result<CampaignState>.Failure("CAMPAIGN022_ECHO_EQUIPPED_INVOCATION_ARTIFACT_REQUIRED");
                var categoryAvailable=battle.Selections.Any(selection=>
                {
                    var forecast=battle.CommittedForecasts.FirstOrDefault(x=>StringComparer.Ordinal.Equals(x.UnionId,selection.UnionId)&&StringComparer.Ordinal.Equals(x.ForecastId,selection.ForecastId));
                    var equipped=equippedByUnion.FirstOrDefault(x=>StringComparer.Ordinal.Equals(x.UnionId,selection.UnionId));
                    if(forecast==null||equipped==null)return false;
                    if(equipped.SpecialRule!=null)return EchoCategoryMatchesForecast(equipped.SpecialRule.Role,forecast);
                    return registry.Echoes.Values.Any(x=>IsCanonicalEcho(x)&&x.unlockFloor<=highestFloor&&EchoCategoryMatchesForecast(x.forecastCategory,forecast));
                });
                return Result<CampaignState>.Failure(categoryAvailable?"CAMPAIGN022_ECHO_RESOURCE_BUDGET_REQUIRED":"CAMPAIGN022_ECHO_FORECAST_CATEGORY_REQUIRED");
            }
            var invocation=EchoInvocationForecastAuthority022.Create(campaign,battle,candidate.Forecast,candidate.Echo,candidate.Artifact.InstanceId,candidate.Action.ActorMemberId);
            if(state.AppliedReceiptIds.Contains(invocation.ReceiptId))return Result<CampaignState>.Failure("CAMPAIGN022_ECHO_RECEIPT_STATE_INCONSISTENT");
            var committedForecast=ApplyEchoToCompleteForecast(candidate.Forecast,candidate.Action,invocation);
            var forecasts=new List<BattleForecastState>(battle.CommittedForecasts);
            var forecastIndex=forecasts.FindIndex(x=>StringComparer.Ordinal.Equals(x.UnionId,committedForecast.UnionId)&&StringComparer.Ordinal.Equals(x.ForecastId,committedForecast.ForecastId));
            if(forecastIndex<0)return Result<CampaignState>.Failure("CAMPAIGN022_ECHO_SELECTED_COMPLETE_FORECAST_REQUIRED");
            forecasts[forecastIndex]=committedForecast;
            var pending=state.With(lastCheckpointId:EchoInvocationForecastAuthority022.PendingCheckpoint(invocation.ReceiptId));
            var pendingCampaign=Success(campaign,city,strategic,progress,playable,pending);
            if(!pendingCampaign.IsSuccess)return pendingCampaign;
            pendingCampaign=Result<CampaignState>.Success(pendingCampaign.Value.WithBattle(battle.With(committedForecasts:forecasts.AsReadOnly())));
            var resolved=battleCommands.ConfirmRound(pendingCampaign.Value,combatContent);
            if(!resolved.IsSuccess)return Result<CampaignState>.Failure(resolved.Errors.ToArray());
            var eventText=EchoInvocationForecastAuthority022.EventText(candidate.Union.DisplayName,invocation);
            var echoEvents=resolved.Value.Battle.EventLog.Where(x=>StringComparer.Ordinal.Equals(x.EventType,EchoInvocationForecastAuthority022.EventType)&&StringComparer.Ordinal.Equals(x.ArtId,invocation.EchoId)&&StringComparer.Ordinal.Equals(x.MemberId,invocation.InvokerMemberId)&&StringComparer.Ordinal.Equals(x.Text,eventText)&&x.Amount>0).ToArray();
            if(echoEvents.Length!=1)return Result<CampaignState>.Failure("CAMPAIGN022_ECHO_BATTLE_EVIDENCE_REQUIRED");
            if(!TryContext(resolved.Value,out city,out strategic,out progress,out playable,out state,out error))return Result<CampaignState>.Failure(error??"CAMPAIGN022_INPUT_REQUIRED");
            if(!StringComparer.Ordinal.Equals(state.LastCheckpointId,EchoInvocationForecastAuthority022.PendingCheckpoint(invocation.ReceiptId))||state.AppliedReceiptIds.Count(x=>StringComparer.Ordinal.Equals(x,invocation.ReceiptId))!=0)return Result<CampaignState>.Failure("CAMPAIGN022_ECHO_PENDING_AUTHORITY_REQUIRED");
            var resolvedUnion=resolved.Value.Battle?.PlayerUnions.FirstOrDefault(x=>StringComparer.Ordinal.Equals(x.UnionId,invocation.UnionId));
            var resolvedArtifact=FindEquippedInvocationArtifact(resolved.Value,state,registry,resolvedUnion,HighestClearedAbyssFloor(state,registry));
            if(resolvedArtifact==null||!StringComparer.Ordinal.Equals(resolvedArtifact.RecruitId,invocation.InvokerMemberId)||!StringComparer.Ordinal.Equals(resolvedArtifact.Artifact.InstanceId,invocation.ArtifactInstanceId))return Result<CampaignState>.Failure("CAMPAIGN022_ECHO_ARTIFACT_AUTHORITY_CHANGED");
            if(candidate.SpecialRule!=null&&(resolvedArtifact.SpecialRule==null||!StringComparer.Ordinal.Equals(resolvedArtifact.SpecialRule.RelicId,candidate.SpecialRule.RelicId)))return Result<CampaignState>.Failure("SPECIAL_RELIC001_INVOCATION_AUTHORITY_CHANGED");
            var artifacts=new List<InvocationArtifactState022>(state.InvocationArtifacts);
            var artifactMatches=artifacts.Count(x=>StringComparer.Ordinal.Equals(x.InstanceId,invocation.ArtifactInstanceId));var artifactIndex=artifacts.FindIndex(x=>StringComparer.Ordinal.Equals(x.InstanceId,invocation.ArtifactInstanceId));
            if(artifactMatches!=1||artifactIndex<0)return Result<CampaignState>.Failure("CAMPAIGN022_ECHO_ARTIFACT_STATE_REQUIRED");
            artifacts[artifactIndex]=artifacts[artifactIndex].With(resonance:checked(artifacts[artifactIndex].Resonance+1));
            var equipmentEvolution=new List<EquipmentEvolutionState022>(state.EquipmentEvolution);
            if(candidate.SpecialRule!=null)
            {
                if(!registry.WeaponTracks.TryGetValue(candidate.SpecialRule.EquipmentTrackId,out var specialTrack)||specialTrack==null)
                    return Result<CampaignState>.Failure("SPECIAL_RELIC001_EQUIPMENT_TRACK_INVALID");
                var evolutionMatches=equipmentEvolution.Count(x=>StringComparer.Ordinal.Equals(x.ItemInstanceId,invocation.ArtifactInstanceId));
                if(evolutionMatches>1)return Result<CampaignState>.Failure("SPECIAL_RELIC001_EQUIPMENT_EVOLUTION_AMBIGUOUS");
                var evolutionIndex=equipmentEvolution.FindIndex(x=>StringComparer.Ordinal.Equals(x.ItemInstanceId,invocation.ArtifactInstanceId));
                var evolution=evolutionIndex>=0
                    ? equipmentEvolution[evolutionIndex]
                    : new EquipmentEvolutionState022(invocation.ArtifactInstanceId,candidate.SpecialRule.EquipmentTrackId,"TRAINING",0,0,Array.Empty<string>(),string.Empty);
                if(!StringComparer.Ordinal.Equals(evolution.TrackId,candidate.SpecialRule.EquipmentTrackId))
                    return Result<CampaignState>.Failure("SPECIAL_RELIC001_EQUIPMENT_TRACK_IMMUTABLE");
                if(HasExactReceiptHistory(evolution.HistoryTag,invocation.ReceiptId))
                    return Result<CampaignState>.Failure("SPECIAL_RELIC001_INVOCATION_RECEIPT_STATE_INCONSISTENT");
                evolution=evolution.With(
                    meaningfulUses:checked(evolution.MeaningfulUses+1),
                    masteryPoints:checked(evolution.MasteryPoints+SpecialRelicInvocationRules001.MeaningfulUseMasteryGain),
                    historyTag:string.IsNullOrWhiteSpace(evolution.HistoryTag)
                        ? invocation.ReceiptId
                        : evolution.HistoryTag+" | "+invocation.ReceiptId);
                if(evolutionIndex>=0)equipmentEvolution[evolutionIndex]=evolution;else equipmentEvolution.Add(evolution);
            }
            var applied=new List<string>(state.AppliedReceiptIds){invocation.ReceiptId};
            var next=state.With(equipmentEvolution:equipmentEvolution.AsReadOnly(),invocationArtifacts:artifacts.AsReadOnly(),appliedReceiptIds:applied.AsReadOnly(),summonResonance:checked(state.SummonResonance+1),lastCheckpointId:"campaign022_echo_invoked:"+invocation.EchoId+":"+invocation.ReceiptId);
            return Success(resolved.Value,city,strategic,progress,playable,next);
        }

        public Result<CampaignState> AdvanceCovenantTrial(CampaignState campaign,ICampaignRegistry022 registry,string covenantId)
        {
            if(!TryContext(campaign,out var city,out var strategic,out var progress,out var playable,out var state,out var error)||registry==null)return Result<CampaignState>.Failure(error??"CAMPAIGN022_INPUT_REQUIRED");
            if(!registry.Covenants.TryGetValue(covenantId,out var def))return Result<CampaignState>.Failure("CAMPAIGN022_COVENANT_UNKNOWN");
            var authorityError=ValidateCovenantAuthority(campaign,strategic,state,registry,def,out var gateReceiptId);if(authorityError!=null)return Result<CampaignState>.Failure(authorityError);
            var list=new List<CovenantProgressState022>(state.Covenants);var matchingCount=list.Count(x=>StringComparer.Ordinal.Equals(x.CovenantId,covenantId));if(matchingCount>1)return Result<CampaignState>.Failure("CAMPAIGN022_COVENANT_TRIAL_STATE_INVALID");var idx=list.FindIndex(x=>StringComparer.Ordinal.Equals(x.CovenantId,covenantId));var current=idx>=0?list[idx]:new CovenantProgressState022(covenantId,CovenantStatus022.TrialAvailable,0,0,Array.Empty<string>());
            if(current.Status==CovenantStatus022.Accepted||current.Status==CovenantStatus022.Refused)return Result<CampaignState>.Failure("CAMPAIGN022_COVENANT_TRIAL_CLOSED");
            if(!HasCanonicalCovenantTrialState(campaign,state,def,current,gateReceiptId))return Result<CampaignState>.Failure("CAMPAIGN022_COVENANT_TRIAL_STATE_INVALID");
            if(current.TrialProgress>=100)return Result<CampaignState>.Success(campaign);
            var stage=current.TrialProgress/CovenantTrialStageProgress+1;var receipt=CovenantTrialReceiptId(campaign,def,stage,gateReceiptId);
            if(current.AppliedReceiptIds.Contains(receipt)||state.AppliedReceiptIds.Contains(receipt))return Result<CampaignState>.Failure("CAMPAIGN022_COVENANT_RECEIPT_STATE_INCONSISTENT");
            var localReceipts=new List<string>(current.AppliedReceiptIds){receipt};var applied=new List<string>(state.AppliedReceiptIds){receipt};
            current=current.With(status:CovenantStatus022.TrialActive,trialProgress:Math.Min(100,current.TrialProgress+CovenantTrialStageProgress),trust:Math.Min(def.trustMaximum,current.Trust+CovenantTrialTrustPerStage),appliedReceiptIds:localReceipts.AsReadOnly());
            if(idx>=0)list[idx]=current;else list.Add(current);
            return Success(campaign,city,strategic,progress,playable,state.With(covenants:list.AsReadOnly(),appliedReceiptIds:applied.AsReadOnly(),lastCheckpointId:"campaign022_covenant_trial:"+receipt));
        }

        public Result<CampaignState> AcceptCovenant(CampaignState campaign,ICampaignRegistry022 registry,string covenantId)
        {
            if(!TryContext(campaign,out var city,out var strategic,out var progress,out var playable,out var state,out var error)||registry==null)return Result<CampaignState>.Failure(error??"CAMPAIGN022_INPUT_REQUIRED");
            if(!registry.Covenants.TryGetValue(covenantId,out var def))return Result<CampaignState>.Failure("CAMPAIGN022_COVENANT_UNKNOWN");
            var authorityError=ValidateCovenantAuthority(campaign,strategic,state,registry,def,out var gateReceiptId);if(authorityError!=null)return Result<CampaignState>.Failure(authorityError);
            var list=new List<CovenantProgressState022>(state.Covenants);if(list.Count(x=>StringComparer.Ordinal.Equals(x.CovenantId,covenantId))!=1)return Result<CampaignState>.Failure("CAMPAIGN022_COVENANT_TRIAL_NOT_READY");var idx=list.FindIndex(x=>StringComparer.Ordinal.Equals(x.CovenantId,covenantId));var current=list[idx];
            if(current.Status==CovenantStatus022.Refused)return Result<CampaignState>.Failure("CAMPAIGN022_COVENANT_REFUSAL_PRESERVED");
            if(!HasCanonicalCovenantTrialState(campaign,state,def,current,gateReceiptId))return Result<CampaignState>.Failure("CAMPAIGN022_COVENANT_TRIAL_STATE_INVALID");
            if(current.TrialProgress<100)return Result<CampaignState>.Failure("CAMPAIGN022_COVENANT_TRIAL_NOT_READY");
            var receipt=CovenantAcceptanceReceiptId(campaign,def,gateReceiptId);
            if(current.Status==CovenantStatus022.Accepted)
                return current.AppliedReceiptIds.Count(x=>StringComparer.Ordinal.Equals(x,receipt))==1&&state.AppliedReceiptIds.Count(x=>StringComparer.Ordinal.Equals(x,receipt))==1?Result<CampaignState>.Success(campaign):Result<CampaignState>.Failure("CAMPAIGN022_COVENANT_RECEIPT_STATE_INCONSISTENT");
            if(current.AppliedReceiptIds.Contains(receipt)||state.AppliedReceiptIds.Contains(receipt))return Result<CampaignState>.Failure("CAMPAIGN022_COVENANT_RECEIPT_STATE_INCONSISTENT");
            var localReceipts=new List<string>(current.AppliedReceiptIds){receipt};var applied=new List<string>(state.AppliedReceiptIds){receipt};current=current.With(status:CovenantStatus022.Accepted,appliedReceiptIds:localReceipts.AsReadOnly());list[idx]=current;
            return Success(campaign,city,strategic,progress,playable,state.With(covenants:list.AsReadOnly(),appliedReceiptIds:applied.AsReadOnly(),lastCheckpointId:"campaign022_covenant_accepted:"+receipt));
        }

        public bool HasEarnedGreatCovenantGate(CampaignState campaign,ICampaignRegistry022 registry)
        {
            return registry!=null&&TryContext(campaign,out _,out var strategic,out _,out _,out var state,out _)&&TryGetCanonicalGreatCovenantGateReceipt(campaign,strategic,state,registry,out _);
        }

        internal bool HasCanonicalAcceptedCovenantAuthority(CampaignState campaign,ICampaignRegistry022 registry,string covenantId,out CovenantDto022 definition,out string acceptanceReceiptId)
        {
            definition=null;acceptanceReceiptId=string.Empty;if(registry==null||string.IsNullOrWhiteSpace(covenantId)||!registry.Covenants.TryGetValue(covenantId,out definition)||!TryContext(campaign,out _,out var strategic,out _,out _,out var state,out _))return false;var authorityError=ValidateCovenantAuthority(campaign,strategic,state,registry,definition,out var gateReceiptId);if(authorityError!=null)return false;var matches=state.Covenants.Where(x=>StringComparer.Ordinal.Equals(x.CovenantId,covenantId)).ToArray();if(matches.Length!=1||matches[0].Status!=CovenantStatus022.Accepted||!HasCanonicalCovenantTrialState(campaign,state,definition,matches[0],gateReceiptId))return false;var expectedAcceptanceReceiptId=CovenantAcceptanceReceiptId(campaign,definition,gateReceiptId);acceptanceReceiptId=expectedAcceptanceReceiptId;return matches[0].AppliedReceiptIds.Count(x=>StringComparer.Ordinal.Equals(x,expectedAcceptanceReceiptId))==1&&state.AppliedReceiptIds.Count(x=>StringComparer.Ordinal.Equals(x,expectedAcceptanceReceiptId))==1;
        }

        static bool HasCompletedEveryAbyssStep(AbyssOperationState022 active,AbyssOperationDto022 op)
        {
            if(active.CurrentStepIndex!=op.steps.Length||active.CompletedStepIds.Count!=op.steps.Length)return false;
            for(var i=0;i<op.steps.Length;i++)if(!StringComparer.Ordinal.Equals(active.CompletedStepIds[i],op.steps[i].stepId))return false;
            return true;
        }

        static bool HasRequiredAbyssBattleAuthority(CampaignState campaign,CampaignProgressionState022 state,AbyssOperationState022 active,AbyssOperationDto022 op)
        {
            var battleSteps=op.steps.Where(x=>x.requiresBattle).ToArray();
            if(battleSteps.Length==0)return true;
            if(battleSteps.Length!=1||campaign?.Battle==null||campaign.Battle.Phase!=BattlePhase.Resolved||campaign.Battle.Reward==null||!campaign.Battle.Reward.Claimed)return false;
            if(campaign.Battle.Outcome!=BattleOutcome.Victory||campaign.Battle.Reward.Outcome!=BattleOutcome.Victory)return false;
            if(string.IsNullOrWhiteSpace(active.ExistingBattleRewardReceiptId)||!StringComparer.Ordinal.Equals(active.ExistingBattleRewardReceiptId,campaign.Battle.Reward.RewardId)||!campaign.Guild.Development.HasClaimedReward(active.ExistingBattleRewardReceiptId))return false;
            var battleProof=active.AppliedStepProofs.FirstOrDefault(value=>
                value!=null&&StringComparer.Ordinal.Equals(value.StepId,
                    battleSteps[0].stepId));
            if(battleProof==null||string.IsNullOrWhiteSpace(
                   battleProof.BattleReturnAuthorityId)||
               !campaign.Guild.Development.HasAdventureAuthority(
                   battleProof.BattleReturnAuthorityId))return false;
            var receipt=CreateAbyssBattleReceipt(active,op,battleSteps[0],
                campaign.Battle,battleProof.BattleReturnAuthorityId);
            return state.AppliedReceiptIds.Count(x=>StringComparer.Ordinal.Equals(x,receipt.ReceiptId))==1&&campaign.Guild.Development.ClaimedBattleRewardIds.Count(x=>StringComparer.Ordinal.Equals(x,active.ExistingBattleRewardReceiptId))==1;
        }

        static string ValidateAbyssOperationLaw(ICampaignRegistry022 registry,AbyssOperationDto022 op,out AbyssFloorDto022 floor)
        {
            floor=null;if(registry==null||op==null||string.IsNullOrWhiteSpace(op.operationId)||string.IsNullOrWhiteSpace(op.floorId)||!registry.Floors.TryGetValue(op.floorId,out floor))return "CAMPAIGN022_ABYSS_AUTHORED_LAW_INVALID";
            if(op.requiresPreviousFloorClear!=(floor.floor>1))return "CAMPAIGN022_ABYSS_PREVIOUS_FLOOR_LAW_INVALID";
            var floorNumber=floor.floor;if(floorNumber>1&&registry.Floors.Values.Count(x=>x.floor==floorNumber-1)!=1)return "CAMPAIGN022_ABYSS_PREVIOUS_FLOOR_LAW_INVALID";
            if(op.guildXp<=0||op.hallXp<=0||op.summonResonance<0||op.rewardMaterialIds==null||op.rewardMaterialIds.Length==0||op.rewardMaterialIds.Any(string.IsNullOrWhiteSpace))return "CAMPAIGN022_ABYSS_REWARD_LAW_INVALID";
            if(op.rewardMaterialIds.Distinct(StringComparer.Ordinal).Count()!=op.rewardMaterialIds.Length)return "CAMPAIGN022_ABYSS_REWARD_MATERIAL_IDS_NOT_UNIQUE";
            if(!op.exactOnceReceipts||op.steps==null||op.steps.Length==0||op.steps.Any(x=>x==null||!x.exactOnce||string.IsNullOrWhiteSpace(x.stepId))||op.steps.Select(x=>x.stepId).Distinct(StringComparer.Ordinal).Count()!=op.steps.Length)return "CAMPAIGN022_ABYSS_EXACT_ONCE_LAW_INVALID";
            if(!op.existingEquipmentRewardRemainsAuthoritative||op.steps.Count(x=>x.requiresBattle)>1)return "CAMPAIGN022_ABYSS_EXISTING_EQUIPMENT_AUTHORITY_INVALID";
            if(!TowerAdventureRules084.IsCompatible084(op,out var boardError))return "CAMPAIGN022_TOWER_BOARD_UNSUPPORTED:"+boardError;
            return null;
        }

        static IReadOnlyList<string> TowerReceiptIds084(
            IReadOnlyList<string> receiptIds)=>(receiptIds??Array.Empty<string>())
            .Where(value=>value.StartsWith("PROGREC022_",StringComparison.Ordinal)||
                value.StartsWith("ABYSSREC022_",StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal).OrderBy(value=>value,
                StringComparer.Ordinal).ToArray();

        static bool TryAbyssRosterSnapshot084(CampaignState campaign,
            out string rosterIdentity,out IReadOnlyList<string> alliedRecruitIds)
        {
            rosterIdentity=string.Empty;alliedRecruitIds=Array.Empty<string>();
            var recruits=campaign?.Guild?.Recruits?.Where(value=>value!=null)
                .Select(value=>value.RecruitId).ToHashSet(StringComparer.Ordinal);
            if(recruits==null||recruits.Count==0)return false;
            var unions=(campaign.Guild.Unions??Array.Empty<UnionState>())
                .Where(value=>value!=null&&value.Kind==UnionKind.Normal&&
                    value.MemberRecruitIds.Any(recruits.Contains))
                .OrderBy(value=>value.UnionId,StringComparer.Ordinal).Take(10)
                .ToArray();
            if(unions.Length==0)return false;
            var members=new SortedSet<string>(StringComparer.Ordinal);
            var authority=new List<object>();
            foreach(var union in unions)
            {
                var owned=union.MemberRecruitIds.Where(recruits.Contains)
                    .OrderBy(value=>value,StringComparer.Ordinal).ToArray();
                if(owned.Length==0||string.IsNullOrWhiteSpace(union.LeaderRecruitId)||
                   !owned.Contains(union.LeaderRecruitId))return false;
                foreach(var member in owned)members.Add(member);
                authority.Add(new
                {
                    union.UnionId,union.LeaderRecruitId,Members=owned,
                    union.FormationId,union.DoctrineId,
                    Positions=union.MemberPositions.Select(value=>new
                    {
                        value.SlotIndex,value.PositionId,value.RecruitId
                    }).ToArray()
                });
            }
            alliedRecruitIds=members.ToArray();
            rosterIdentity=CanonicalJson.Sha256Hex(new
            {
                Authority="ABYSS_ALLIED_ROSTER_022_2.0",Unions=authority,
                AlliedRecruitIds=alliedRecruitIds
            });
            return alliedRecruitIds.Count>0;
        }

        static CampaignProgressionState022 EnsureAbyssAuthority084(
            CampaignState campaign,CampaignProgressionState022 state)
        {
            if(campaign==null||state==null||
               !string.IsNullOrWhiteSpace(state.AbyssAuthorityHash))return state;
            if(state.ActiveAbyssOperation!=null||state.AbyssFloors.Count!=0||
               TowerReceiptIds084(state.AppliedReceiptIds).Count!=0||
               state.AboveGroundChangeIds.Count!=0||
               state.AbyssAuthorityEntries.Count!=0||
               state.ActiveAbyssBeginGrant!=null||
               state.ActiveAbyssStepLedger.Count!=0||
               !string.IsNullOrWhiteSpace(state.AbyssAuthorityBaseHash)||
               !string.IsNullOrWhiteSpace(state.AbyssAuthorityMigrationReceiptId))
                return state;
            var root=AbyssAuthorityRootHash084(campaign,0,
                Array.Empty<AbyssFloorProgressState022>(),Array.Empty<string>(),
                Array.Empty<string>(),string.Empty);
            return state.With(abyssAuthorityBaseHash:root,
                abyssAuthorityBaseOperationOrdinal:0,
                abyssAuthorityBaseFloors:Array.Empty<AbyssFloorProgressState022>(),
                abyssAuthorityBaseReceiptIds:Array.Empty<string>(),
                abyssAuthorityBaseAboveGroundChangeIds:Array.Empty<string>(),
                abyssAuthorityEntries:Array.Empty<AbyssAuthorityEntry022>(),
                abyssAuthorityHash:root,abyssAuthorityMigrationReceiptId:string.Empty);
        }

        static string AbyssAuthorityRootHash084(CampaignState campaign,int ordinal,
            IReadOnlyList<AbyssFloorProgressState022> floors,
            IReadOnlyList<string> receipts,IReadOnlyList<string> changes,
            string migrationReceiptId)=>CanonicalJson.Sha256Hex(new
            {
                Authority="ABYSS_AUTHORITY_ROOT_022_2.0",campaign.CampaignGuid,
                campaign.CampaignSeed,BaseOperationOrdinal=ordinal,
                BaseFloors=floors,BaseReceiptIds=receipts,
                BaseAboveGroundChangeIds=changes,
                MigrationReceiptId=migrationReceiptId
            });

        static string AbyssMigrationReceiptId084(CampaignState campaign,
            int ordinal,IReadOnlyList<AbyssFloorProgressState022> floors,
            IReadOnlyList<string> receipts,IReadOnlyList<string> changes)=>
            "ABYSSMIGRATE022_"+CanonicalJson.Sha256Hex(new
            {
                Authority="ABYSS_LEGACY_MIGRATION_022_2.0",
                campaign.CampaignGuid,campaign.CampaignSeed,
                BaseOperationOrdinal=ordinal,BaseFloors=floors,
                BaseReceiptIds=receipts,BaseAboveGroundChangeIds=changes
            }).Substring(0,24).ToUpperInvariant();

        static string AbyssCompletionProofHash084(AbyssFirstClearProof022 proof)=>
            CanonicalJson.Sha256Hex(new
            {
                proof.CampaignGuid,proof.CampaignSeed,
                proof.AppliedReceiptCountAtBegin,proof.OperationInstanceId,
                proof.OperationDefinitionId,proof.FloorId,
                proof.CanonicalSeedIdentity,proof.CompletedStepIds,
                proof.StepReceipts,proof.ExistingBattleRewardReceiptId,
                proof.CompletionReceipt,proof.CommittedOperationOrdinal,
                proof.PreviousAuthorityHash,proof.BeginAuthorityHash,
                proof.AlliedRosterIdentity,proof.AlliedRecruitIds,
                proof.FloorClearCountAtBegin
            });

        static string AbyssAuthorityEntryHash084(AbyssAuthorityEntry022 entry)=>
            entry.TowerRestart130==null?AbyssOriginalEntryHash084(entry):
            CanonicalJson.Sha256Hex(new { Policy=TowerRestartPolicy130,
                OriginalEntryHash=AbyssOriginalEntryHash084(entry),entry.TowerRestart130 });

        // Retain the exact historical hash shape when the optional proof is absent.
        static string AbyssOriginalEntryHash084(AbyssAuthorityEntry022 entry)=>
            CanonicalJson.Sha256Hex(new
            {
                entry.PreviousHash,entry.SequenceIndex,
                entry.CommittedOperationOrdinal,entry.OperationInstanceId,
                entry.OperationDefinitionId,entry.FloorId,
                entry.Aborted,
                CompletionProofHash=entry.CompletionProof==null?string.Empty:
                    AbyssCompletionProofHash084(entry.CompletionProof),
                entry.AbortedOperation
            });

        static AbyssAuthorityEntry022 CreateAbyssAuthorityEntry084(
            CampaignProgressionState022 state,AbyssFirstClearProof022 proof)
        {
            if(state==null||proof==null||
               !StringComparer.Ordinal.Equals(proof.PreviousAuthorityHash,
                   state.AbyssAuthorityHash))return null;
            AbyssAuthorityEntry022 Build(string hash)=>new AbyssAuthorityEntry022(
                state.AbyssAuthorityHash,hash,state.AbyssAuthorityEntries.Count+1,
                proof.CommittedOperationOrdinal,proof.OperationInstanceId,
                proof.OperationDefinitionId,proof.FloorId,proof);
            var draft=Build("PENDING_ABYSS_AUTHORITY_HASH_022");
            return Build(AbyssAuthorityEntryHash084(draft));
        }

        static AbyssAuthorityEntry022 CreateAbyssAbortEntry084(
            CampaignProgressionState022 state,AbyssOperationState022 operation,
            TowerRestartDefeatProof130 restartProof=null)
        {
            if(state==null||operation==null||
               !StringComparer.Ordinal.Equals(operation.PreviousAuthorityHash,
                   state.AbyssAuthorityHash)||
               state.ActiveAbyssBeginGrant==null||
               !GrantMatchesActive084(state.ActiveAbyssBeginGrant,operation)||
               !SameStepProofs084(state.ActiveAbyssStepLedger,
                   operation.AppliedStepProofs))return null;
            AbyssAuthorityEntry022 Build(string hash)=>new AbyssAuthorityEntry022(
                state.AbyssAuthorityHash,hash,state.AbyssAuthorityEntries.Count+1,
                operation.CommittedOperationOrdinal,
                operation.OperationInstanceId,operation.OperationDefinitionId,
                operation.FloorId,null,true,operation,restartProof);
            var draft=Build("PENDING_ABYSS_ABORT_AUTHORITY_HASH_022");
            return Build(AbyssAuthorityEntryHash084(draft));
        }

        // Fresh indexes for this one validation only. They preserve cardinality
        // checks and Ordinal membership; all proof/hash/registry checks still run.
        // No campaign, command result, or validation outcome is cached.
        sealed class AbyssValidationLedgerIndex110
        {
            readonly Dictionary<string,int> _applied=new Dictionary<string,int>(StringComparer.Ordinal);
            readonly Dictionary<string,int> _returns=new Dictionary<string,int>(StringComparer.Ordinal);
            readonly HashSet<string> _claimed;
            readonly HashSet<string> _authorities;
            readonly int _appliedNulls,_returnNulls;

            public AbyssValidationLedgerIndex110(CampaignState campaign,CampaignProgressionState022 state)
                :this(state.AppliedReceiptIds,campaign.Guild.GuildCity.AppliedBattleReturnIds,
                    campaign.Guild.Development.ClaimedBattleRewardIds,
                    campaign.Guild.Development.AppliedAdventureAuthorityIds) { }

            internal AbyssValidationLedgerIndex110(IReadOnlyList<string> applied,
                IReadOnlyList<string> returns,IReadOnlyList<string> claimed,IReadOnlyList<string> authorities)
            {
                _appliedNulls=CountIds(applied,_applied);
                _returnNulls=CountIds(returns,_returns);
                _claimed=new HashSet<string>(claimed,StringComparer.Ordinal);
                _authorities=new HashSet<string>(authorities,StringComparer.Ordinal);
            }
            static int CountIds(IReadOnlyList<string> source,Dictionary<string,int> counts)
            {
                var nulls=0;
                for(var index=0;index<source.Count;index++)
                {
                    var id=source[index];
                    if(id==null){nulls++;continue;}
                    counts.TryGetValue(id,out var count);counts[id]=count+1;
                }
                return nulls;
            }
            public int AppliedCount(string id)=>id==null?_appliedNulls:
                _applied.TryGetValue(id,out var count)?count:0;
            public int BattleReturnCount(string id)=>id==null?_returnNulls:
                _returns.TryGetValue(id,out var count)?count:0;
            public bool HasClaimedReward(string id)=>!string.IsNullOrWhiteSpace(id)&&_claimed.Contains(id);
            public bool HasAdventureAuthority(string id)=>!string.IsNullOrWhiteSpace(id)&&_authorities.Contains(id);
        }

        static bool ValidateAbyssAuthority084(CampaignState campaign,
            ICampaignRegistry022 registry,CampaignProgressionState022 state)
        {
            if(campaign==null||registry==null||state==null||
               !StringComparer.Ordinal.Equals(state.ContentAuthorityVersion,
                   CampaignProgressionState022.ContentVersion)||
               string.IsNullOrWhiteSpace(state.AbyssAuthorityBaseHash)||
               string.IsNullOrWhiteSpace(state.AbyssAuthorityHash))return false;
            var migrated=!string.IsNullOrWhiteSpace(
                state.AbyssAuthorityMigrationReceiptId);
            if(!migrated&&(state.AbyssAuthorityBaseOperationOrdinal!=0||
               state.AbyssAuthorityBaseFloors.Count!=0||
               state.AbyssAuthorityBaseReceiptIds.Count!=0||
               state.AbyssAuthorityBaseAboveGroundChangeIds.Count!=0))return false;
            if(migrated&&!StringComparer.Ordinal.Equals(
                   state.AbyssAuthorityMigrationReceiptId,
                   AbyssMigrationReceiptId084(campaign,
                       state.AbyssAuthorityBaseOperationOrdinal,
                       state.AbyssAuthorityBaseFloors,
                       state.AbyssAuthorityBaseReceiptIds,
                       state.AbyssAuthorityBaseAboveGroundChangeIds)))return false;
            var expectedRoot=AbyssAuthorityRootHash084(campaign,
                state.AbyssAuthorityBaseOperationOrdinal,
                state.AbyssAuthorityBaseFloors,
                state.AbyssAuthorityBaseReceiptIds,
                state.AbyssAuthorityBaseAboveGroundChangeIds,
                state.AbyssAuthorityMigrationReceiptId);
            if(!StringComparer.Ordinal.Equals(expectedRoot,
                   state.AbyssAuthorityBaseHash))return false;
            var ledger110=new AbyssValidationLedgerIndex110(campaign,state);
            var receipts=new HashSet<string>(
                TowerReceiptIds084(state.AbyssAuthorityBaseReceiptIds),
                StringComparer.Ordinal);
            var changes=new HashSet<string>(
                state.AbyssAuthorityBaseAboveGroundChangeIds,
                StringComparer.Ordinal);
            var floors=state.AbyssAuthorityBaseFloors.ToDictionary(
                value=>value.FloorId,value=>value,StringComparer.Ordinal);
            var previous=state.AbyssAuthorityBaseHash;
            var previousOrdinal=state.AbyssAuthorityBaseOperationOrdinal;
            var sequence=0;
            foreach(var entry in state.AbyssAuthorityEntries)
            {
                if(entry==null||entry.SequenceIndex!=sequence+1||
                   entry.CommittedOperationOrdinal<=previousOrdinal||
                   !StringComparer.Ordinal.Equals(entry.PreviousHash,previous)||
                   !StringComparer.Ordinal.Equals(entry.EntryHash,
                       AbyssAuthorityEntryHash084(entry))||
                   !registry.AbyssOperations.TryGetValue(
                       entry.OperationDefinitionId,out var operation)||
                   operation==null||!StringComparer.Ordinal.Equals(
                       operation.floorId,entry.FloorId)||
                   ValidateAbyssOperationLaw(registry,operation,out var floor)!=null)
                    return false;
                var clearCount=floors.TryGetValue(entry.FloorId,out var existing)
                    ?existing.ClearCount:0;
                if(entry.Aborted)
                {
                    if(entry.CompletionProof!=null||entry.AbortedOperation==null||
                       !StringComparer.Ordinal.Equals(
                           entry.AbortedOperation.PreviousAuthorityHash,previous)||
                       entry.AbortedOperation.CommittedOperationOrdinal!=
                           entry.CommittedOperationOrdinal||
                       entry.AbortedOperation.AppliedReceiptCountAtBegin!=
                           receipts.Count||
                       entry.AbortedOperation.FloorClearCountAtBegin!=clearCount||
                       !ValidateAbyssAppliedStepPrefix084(campaign,state,operation,
                           entry.AbortedOperation,ledger110)||
                       !ValidateTowerRestartEntry130(campaign,registry,entry))return false;
                    foreach(var stepProof in entry.AbortedOperation.AppliedStepProofs)
                        if(!receipts.Add(stepProof.Receipt.ReceiptId))return false;
                    previous=entry.EntryHash;
                    previousOrdinal=entry.CommittedOperationOrdinal;
                    sequence=entry.SequenceIndex;
                    continue;
                }
                if(!ValidateAbyssCompletionProof084(campaign,state,operation,
                       entry.CompletionProof,previous,receipts.Count,clearCount,ledger110))
                    return false;
                foreach(var stepProof in entry.CompletionProof.StepReceipts)
                    if(!receipts.Add(stepProof.Receipt.ReceiptId))return false;
                if(!receipts.Add(
                       entry.CompletionProof.CompletionReceipt.ReceiptId))return false;
                var floorReceipts=new List<string>(existing?.AppliedReceiptIds??
                    Array.Empty<string>())
                    {entry.CompletionProof.CompletionReceipt.ReceiptId};
                floors[entry.FloorId]=new AbyssFloorProgressState022(
                    entry.FloorId,clearCount+1,Math.Max(
                        existing?.HighestJudgmentTier??0,1),true,
                    existing?.MarginResolved??false,floorReceipts.AsReadOnly(),
                    clearCount==0?entry.CompletionProof:existing.FirstClearProof);
                changes.Add(floor.firstClearAboveGroundChangeId);
                previous=entry.EntryHash;previousOrdinal=
                    entry.CommittedOperationOrdinal;sequence=entry.SequenceIndex;
            }
            if(!StringComparer.Ordinal.Equals(previous,state.AbyssAuthorityHash))
                return false;
            var active=state.ActiveAbyssOperation;
            if(active!=null)
            {
                var grant=state.ActiveAbyssBeginGrant;
                if(grant==null||!GrantMatchesActive084(grant,active)||
                   !SameStepProofs084(state.ActiveAbyssStepLedger,
                       active.AppliedStepProofs))return false;
                if(!StringComparer.Ordinal.Equals(active.PreviousAuthorityHash,
                       previous)||active.CommittedOperationOrdinal<=previousOrdinal||
                   !registry.AbyssOperations.TryGetValue(
                       active.OperationDefinitionId,out var activeDefinition)||
                   activeDefinition==null||
                   active.FloorClearCountAtBegin!=(floors.TryGetValue(
                       active.FloorId,out var activeFloor)?activeFloor.ClearCount:0)||
                   active.AppliedReceiptCountAtBegin!=receipts.Count||
                   !ValidateAbyssAppliedStepPrefix084(campaign,state,
                       activeDefinition,active,ledger110))return false;
                foreach(var stepProof in active.AppliedStepProofs)
                    if(stepProof?.Receipt==null||
                       !receipts.Add(stepProof.Receipt.ReceiptId))return false;
            }
            else if(state.ActiveAbyssBeginGrant!=null||
                    state.ActiveAbyssStepLedger.Count!=0)return false;
            return SameIds084(receipts.ToArray(),
                       TowerReceiptIds084(state.AppliedReceiptIds))&&
                   SameFloorAuthority084(floors.Values.ToArray(),
                       state.AbyssFloors)&&
                   SameIds084(changes.ToArray(),state.AboveGroundChangeIds)&&
                   ValidateTowerFloorBindings094(campaign,registry,state);
        }

        static bool ValidateAbyssCompletionProof084(CampaignState campaign,
            CampaignProgressionState022 state,AbyssOperationDto022 operation,
            AbyssFirstClearProof022 proof,string expectedPreviousHash,
            int expectedReceiptCount,int expectedFloorClearCount,
            AbyssValidationLedgerIndex110 ledger110)
        {
            if(campaign==null||state==null||operation==null||proof==null||
               !StringComparer.Ordinal.Equals(proof.CampaignGuid,
                   campaign.CampaignGuid)||proof.CampaignSeed!=campaign.CampaignSeed||
               !StringComparer.Ordinal.Equals(proof.OperationDefinitionId,
                   operation.operationId)||!StringComparer.Ordinal.Equals(
                       proof.FloorId,operation.floorId)||
               !StringComparer.Ordinal.Equals(proof.PreviousAuthorityHash,
                   expectedPreviousHash)||
               proof.AppliedReceiptCountAtBegin!=expectedReceiptCount||
               proof.FloorClearCountAtBegin!=expectedFloorClearCount||
               proof.CommittedOperationOrdinal<=0||
               proof.CompletedStepIds.Count!=operation.steps.Length||
               proof.StepReceipts.Count!=operation.steps.Length||
               string.IsNullOrWhiteSpace(proof.AlliedRosterIdentity)||
               proof.AlliedRecruitIds.Count==0)return false;
            var reconstructed=new AbyssOperationState022(
                proof.OperationInstanceId,proof.OperationDefinitionId,
                proof.FloorId,operation.steps.Length,
                AbyssOperationStatus022.ReadyToFinalize,proof.CompletedStepIds,
                proof.CompletionReceipt,proof.ExistingBattleRewardReceiptId,
                proof.CanonicalSeedIdentity,proof.AppliedReceiptCountAtBegin,
                proof.StepReceipts,proof.CommittedOperationOrdinal,
                proof.PreviousAuthorityHash,proof.BeginAuthorityHash,
                proof.AlliedRosterIdentity,proof.AlliedRecruitIds,
                proof.FloorClearCountAtBegin);
            if(!HasCanonicalAbyssOperationIdentity084(campaign,reconstructed)||
               !HasCompletedEveryAbyssStep(reconstructed,operation))return false;
            var battleCount=0;
            for(var index=0;index<operation.steps.Length;index++)
            {
                var step=operation.steps[index];var stepProof=proof.StepReceipts[index];
                if(stepProof==null||stepProof.Receipt==null||
                   !StringComparer.Ordinal.Equals(stepProof.StepId,step.stepId)||
                   stepProof.RequiresBattle!=step.requiresBattle||
                   !StringComparer.Ordinal.Equals(proof.CompletedStepIds[index],
                       step.stepId)||ledger110.AppliedCount(stepProof.Receipt.ReceiptId)!=1)return false;
                ProgressionReceipt022 expected;
                if(step.requiresBattle)
                {
                    battleCount++;
                    if(!StringComparer.Ordinal.Equals(stepProof.Outcome,"VICTORY")||
                       string.IsNullOrWhiteSpace(stepProof.BattleFinalStateHash)||
                       string.IsNullOrWhiteSpace(
                           stepProof.ExistingBattleRewardReceiptId)||
                       string.IsNullOrWhiteSpace(stepProof.BattleRequestId)||
                       string.IsNullOrWhiteSpace(stepProof.BattleId)||
                       string.IsNullOrWhiteSpace(stepProof.BattleReturnReceiptId)||
                       string.IsNullOrWhiteSpace(stepProof.ReturnCheckpointId)||
                       !stepProof.BattleRequestId.StartsWith(
                           "ABYSS_ENCOUNTER022_",StringComparison.Ordinal)||
                       !stepProof.BattleId.EndsWith(
                           stepProof.BattleRequestId.Substring(
                               "ABYSS_ENCOUNTER022_".Length),
                           StringComparison.Ordinal)||
                       !ledger110.HasClaimedReward(
                           stepProof.ExistingBattleRewardReceiptId)||
                        string.IsNullOrWhiteSpace(stepProof.BattleReturnAuthorityId)||
                        !ledger110.HasAdventureAuthority(
                            stepProof.BattleReturnAuthorityId)||
                        !StringComparer.Ordinal.Equals(
                            proof.ExistingBattleRewardReceiptId,
                           stepProof.ExistingBattleRewardReceiptId))return false;
                    var returnHash=CanonicalJson.Sha256Hex(new
                    {
                        reconstructed.OperationInstanceId,step.stepId,
                        RequestId=stepProof.BattleRequestId,
                        BattleId=stepProof.BattleId,
                        FinalStateHash=stepProof.BattleFinalStateHash,
                        Outcome=BattleOutcome.Victory,
                        RewardId=stepProof.ExistingBattleRewardReceiptId,
                        ReturnCheckpointId=stepProof.ReturnCheckpointId
                    });
                    var expectedReturn="ABYSS_RETURN022_"+
                        returnHash.Substring(0,24).ToUpperInvariant();
                    if(!StringComparer.Ordinal.Equals(
                           stepProof.BattleReturnReceiptId,expectedReturn)||
                       ledger110.BattleReturnCount(expectedReturn)!=1)
                        return false;
                    expected=CreateAbyssBattleReceipt(reconstructed,operation,step,
                        stepProof.BattleFinalStateHash,
                        stepProof.ExistingBattleRewardReceiptId,
                        stepProof.BattleReturnAuthorityId);
                }
                else
                {
                    if(!string.IsNullOrWhiteSpace(stepProof.BattleFinalStateHash)||
                       !string.IsNullOrWhiteSpace(
                           stepProof.ExistingBattleRewardReceiptId)||
                       !string.IsNullOrWhiteSpace(stepProof.BattleRequestId)||
                       !string.IsNullOrWhiteSpace(stepProof.BattleId)||
                       !string.IsNullOrWhiteSpace(
                           stepProof.BattleReturnReceiptId)||
                        !string.IsNullOrWhiteSpace(stepProof.ReturnCheckpointId)||
                        !string.IsNullOrWhiteSpace(
                            stepProof.BattleReturnAuthorityId))
                        return false;
                    expected=CreateAbyssNonBattleStepReceipt(reconstructed,step,
                        stepProof.Outcome);
                }
                if(!ReceiptsMatch(stepProof.Receipt,expected))return false;
            }
            var expectedCompletion=CreateAbyssOperationCompletionReceipt(
                reconstructed,operation);
            return battleCount==operation.steps.Count(value=>value.requiresBattle)&&
                   ReceiptsMatch(proof.CompletionReceipt,expectedCompletion)&&
                   proof.CompletionReceipt.AppliedVersion==1&&
                   StringComparer.Ordinal.Equals(proof.CompletionReceipt.Outcome,
                       "OPERATION_COMPLETE")&&
                   ledger110.AppliedCount(proof.CompletionReceipt.ReceiptId)==1&&
                   ledger110.HasClaimedReward(
                       proof.CompletionReceipt.ReceiptId);
        }

        static bool ValidateAbyssAppliedStepPrefix084(CampaignState campaign,
            CampaignProgressionState022 state,AbyssOperationDto022 operation,
            AbyssOperationState022 active,AbyssValidationLedgerIndex110 ledger110)
        {
            if(campaign==null||state==null||operation==null||active==null||
               !HasCanonicalAbyssOperationIdentity084(campaign,active)||
               active.CurrentStepIndex!=active.CompletedStepIds.Count||
               active.CurrentStepIndex!=active.AppliedStepProofs.Count||
               active.CurrentStepIndex<0||
               active.CurrentStepIndex>operation.steps.Length)return false;
            for(var index=0;index<active.CurrentStepIndex;index++)
            {
                var step=operation.steps[index];var proof=active.AppliedStepProofs[index];
                if(proof==null||proof.Receipt==null||
                   !StringComparer.Ordinal.Equals(active.CompletedStepIds[index],
                       step.stepId)||!StringComparer.Ordinal.Equals(proof.StepId,
                           step.stepId)||proof.RequiresBattle!=step.requiresBattle||
                    ledger110.AppliedCount(proof.Receipt.ReceiptId)!=1||
                    !ledger110.HasAdventureAuthority(
                        proof.Receipt.ReceiptId))return false;
                ProgressionReceipt022 expected;
                if(step.requiresBattle)
                {
                    if(!StringComparer.Ordinal.Equals(proof.Outcome,"VICTORY")||
                       string.IsNullOrWhiteSpace(proof.BattleFinalStateHash)||
                       string.IsNullOrWhiteSpace(
                           proof.ExistingBattleRewardReceiptId)||
                       !ledger110.HasClaimedReward(
                           proof.ExistingBattleRewardReceiptId)||
                       !StringComparer.Ordinal.Equals(
                           active.ExistingBattleRewardReceiptId,
                           proof.ExistingBattleRewardReceiptId))return false;
                    var returnHash=CanonicalJson.Sha256Hex(new
                    {
                        active.OperationInstanceId,step.stepId,
                        RequestId=proof.BattleRequestId,BattleId=proof.BattleId,
                        FinalStateHash=proof.BattleFinalStateHash,
                        Outcome=BattleOutcome.Victory,
                        RewardId=proof.ExistingBattleRewardReceiptId,
                        ReturnCheckpointId=proof.ReturnCheckpointId
                    });
                    var expectedReturn="ABYSS_RETURN022_"+
                        returnHash.Substring(0,24).ToUpperInvariant();
                    if(!StringComparer.Ordinal.Equals(proof.BattleReturnReceiptId,
                           expectedReturn)||
                       ledger110.BattleReturnCount(expectedReturn)!=1)
                        return false;
                    if(string.IsNullOrWhiteSpace(proof.BattleReturnAuthorityId)||
                       !ledger110.HasAdventureAuthority(
                           proof.BattleReturnAuthorityId))return false;
                    expected=CreateAbyssBattleReceipt(active,operation,step,
                        proof.BattleFinalStateHash,
                        proof.ExistingBattleRewardReceiptId,
                        proof.BattleReturnAuthorityId);
                }
                else
                {
                    if(!string.IsNullOrWhiteSpace(proof.BattleFinalStateHash)||
                       !string.IsNullOrWhiteSpace(
                           proof.ExistingBattleRewardReceiptId)||
                       !string.IsNullOrWhiteSpace(proof.BattleReturnReceiptId)||
                       !string.IsNullOrWhiteSpace(proof.BattleRequestId)||
                       !string.IsNullOrWhiteSpace(proof.BattleId)||
                        !string.IsNullOrWhiteSpace(proof.ReturnCheckpointId)||
                        !string.IsNullOrWhiteSpace(
                            proof.BattleReturnAuthorityId))return false;
                    expected=CreateAbyssNonBattleStepReceipt(active,step,
                        proof.Outcome);
                }
                if(!ReceiptsMatch(proof.Receipt,expected))return false;
            }
            return true;
        }

        static bool SameIds084(IReadOnlyList<string> first,
            IReadOnlyList<string> second)=>(first??Array.Empty<string>())
            .OrderBy(value=>value,StringComparer.Ordinal).SequenceEqual(
                (second??Array.Empty<string>()).OrderBy(value=>value,
                    StringComparer.Ordinal),StringComparer.Ordinal);

        static bool SameFloorAuthority084(
            IReadOnlyList<AbyssFloorProgressState022> first,
            IReadOnlyList<AbyssFloorProgressState022> second)
        {
            var left=(first??Array.Empty<AbyssFloorProgressState022>())
                .OrderBy(value=>value.FloorId,StringComparer.Ordinal).ToArray();
            var right=(second??Array.Empty<AbyssFloorProgressState022>())
                .OrderBy(value=>value.FloorId,StringComparer.Ordinal).ToArray();
            if(left.Length!=right.Length)return false;
            for(var index=0;index<left.Length;index++)
            {
                var a=left[index];var b=right[index];
                if(a==null||b==null||!StringComparer.Ordinal.Equals(a.FloorId,b.FloorId)||
                   a.ClearCount!=b.ClearCount||
                   a.HighestJudgmentTier!=b.HighestJudgmentTier||
                   a.FirstClearApplied!=b.FirstClearApplied||
                   a.MarginResolved!=b.MarginResolved||
                   !SameIds084(a.AppliedReceiptIds,b.AppliedReceiptIds)||
                   ((a.FirstClearProof==null)!=(b.FirstClearProof==null))||
                   (a.FirstClearProof!=null&&!StringComparer.Ordinal.Equals(
                       AbyssCompletionProofHash084(a.FirstClearProof),
                       AbyssCompletionProofHash084(b.FirstClearProof))))return false;
            }
            return true;
        }

        static bool GrantMatchesActive084(AbyssBeginGrant022 grant,
            AbyssOperationState022 active)=>grant!=null&&active!=null&&
            StringComparer.Ordinal.Equals(grant.BeginAuthorityHash,
                active.BeginAuthorityHash)&&
            StringComparer.Ordinal.Equals(grant.PreviousAuthorityHash,
                active.PreviousAuthorityHash)&&
            grant.CommittedOperationOrdinal==active.CommittedOperationOrdinal&&
            StringComparer.Ordinal.Equals(grant.OperationInstanceId,
                active.OperationInstanceId)&&
            StringComparer.Ordinal.Equals(grant.OperationDefinitionId,
                active.OperationDefinitionId)&&
            StringComparer.Ordinal.Equals(grant.FloorId,active.FloorId)&&
            grant.AppliedReceiptCountAtBegin==active.AppliedReceiptCountAtBegin&&
            grant.FloorClearCountAtBegin==active.FloorClearCountAtBegin&&
            StringComparer.Ordinal.Equals(grant.AlliedRosterIdentity,
                active.AlliedRosterIdentity)&&
            SameIds084(grant.AlliedRecruitIds,active.AlliedRecruitIds);

        static bool SameStepProofs084(
            IReadOnlyList<AbyssStepReceiptProof022> first,
            IReadOnlyList<AbyssStepReceiptProof022> second)
        {
            var left=first??Array.Empty<AbyssStepReceiptProof022>();
            var right=second??Array.Empty<AbyssStepReceiptProof022>();
            if(left.Count!=right.Count)return false;
            for(var index=0;index<left.Count;index++)
                if(!StringComparer.Ordinal.Equals(CanonicalJson.Serialize(left[index]),
                       CanonicalJson.Serialize(right[index])))return false;
            return true;
        }

        static string CreateAbyssCanonicalSeed(CampaignState campaign,string operationId,
            int appliedReceiptCountAtBegin,int committedOperationOrdinal,
            string previousAuthorityHash,int floorClearCountAtBegin,
            string alliedRosterIdentity,IReadOnlyList<string> alliedRecruitIds,
            string beginAuthorityHash)=>CanonicalJson.Sha256Hex(new
            {
                campaign.CampaignGuid,campaign.CampaignSeed,operationId,
                count=appliedReceiptCountAtBegin,committedOperationOrdinal,
                previousAuthorityHash,floorClearCountAtBegin,
                alliedRosterIdentity,alliedRecruitIds,beginAuthorityHash
            });

        static string CreateAbyssBeginAuthority084(CampaignState campaign,
            string operationId,string floorId,int committedOperationOrdinal,
            string previousAuthorityHash,int appliedReceiptCountAtBegin,
            int floorClearCountAtBegin,string alliedRosterIdentity,
            IReadOnlyList<string> alliedRecruitIds)=>CanonicalJson.Sha256Hex(new
            {
                Authority="ABYSS_BEGIN_AUTHORITY_022_2.0",campaign.CampaignGuid,
                campaign.CampaignSeed,operationId,floorId,
                committedOperationOrdinal,previousAuthorityHash,
                appliedReceiptCountAtBegin,floorClearCountAtBegin,
                alliedRosterIdentity,alliedRecruitIds
            });

        static bool HasCanonicalAbyssOperationIdentity084(CampaignState campaign,
            AbyssOperationState022 active)
        {
            if(campaign==null||active==null||
               !active.AppliedReceiptCountAtBegin.HasValue||
               active.CommittedOperationOrdinal<=0||
               active.FloorClearCountAtBegin<0||
               string.IsNullOrWhiteSpace(active.PreviousAuthorityHash)||
               string.IsNullOrWhiteSpace(active.BeginAuthorityHash)||
               string.IsNullOrWhiteSpace(active.AlliedRosterIdentity)||
               active.AlliedRecruitIds.Count==0)return false;
            var expectedBegin=CreateAbyssBeginAuthority084(campaign,
                active.OperationDefinitionId,active.FloorId,
                active.CommittedOperationOrdinal,active.PreviousAuthorityHash,
                active.AppliedReceiptCountAtBegin.Value,
                active.FloorClearCountAtBegin,active.AlliedRosterIdentity,
                active.AlliedRecruitIds);
            var seed=CreateAbyssCanonicalSeed(campaign,
                active.OperationDefinitionId,active.AppliedReceiptCountAtBegin.Value,
                active.CommittedOperationOrdinal,active.PreviousAuthorityHash,
                active.FloorClearCountAtBegin,active.AlliedRosterIdentity,
                active.AlliedRecruitIds,expectedBegin);
            return StringComparer.Ordinal.Equals(active.BeginAuthorityHash,
                       expectedBegin)&&
                   StringComparer.Ordinal.Equals(active.CanonicalSeedIdentity,seed)&&
                   (IsEndlessTowerOperation094(active.OperationInstanceId)
                       ? HasCanonicalTowerOperationIdentity094(campaign,active,seed)
                       : StringComparer.Ordinal.Equals(active.OperationInstanceId,
                           "ABYSSRUN022_"+seed.Substring(0,24).ToUpperInvariant()));
        }

        static bool HasCanonicalActiveAbyssIdentity(CampaignState campaign,
            ICampaignRegistry022 registry,AbyssOperationState022 active)
        {
            var state=campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019?
                .Playable020?.Progression022;
            if(!HasCanonicalAbyssOperationIdentity084(campaign,active)||state==null||
               !ValidateAbyssAuthority084(campaign,registry,state)||
               (campaign.Guild.GuildCity.OperationOrdinal!=
                   active.CommittedOperationOrdinal && !SecondDimension.Gameplay.Navigation164.LoopCheckpoint164.MatchesTower(campaign,active))||
               !StringComparer.Ordinal.Equals(active.PreviousAuthorityHash,
                   state.AbyssAuthorityHash)||
               !TryAbyssRosterSnapshot084(campaign,out var rosterIdentity,
                   out var alliedRecruitIds)||
               !StringComparer.Ordinal.Equals(active.AlliedRosterIdentity,
                   rosterIdentity)||!SameIds084(active.AlliedRecruitIds,
                       alliedRecruitIds))return false;
            return TowerReceiptIds084(state.AppliedReceiptIds).Count==
                       active.AppliedReceiptCountAtBegin.Value+
                       active.AppliedStepProofs.Count;
        }

        static ProgressionReceipt022 CreateAbyssNonBattleStepReceipt(AbyssOperationState022 active,AbyssStepDto022 step,string outcome)
        {
            var canonicalOutcome=outcome??"SUCCESS";var hash=CanonicalJson.Sha256Hex(new{active.OperationInstanceId,step.stepId,outcome=canonicalOutcome,active.CanonicalSeedIdentity});return new ProgressionReceipt022("PROGREC022_"+hash.Substring(0,24).ToUpperInvariant(),step.stepId,canonicalOutcome,hash,0,0,0,Array.Empty<string>(),0);
        }

        static bool HasCanonicalCompletedAbyssStepProof(CampaignState campaign,GuildCityState017D city,CampaignProgressionState022 state,AbyssOperationState022 active,AbyssOperationDto022 op)
        {
            if(op==null||!StringComparer.Ordinal.Equals(active.OperationDefinitionId,op.operationId)||!StringComparer.Ordinal.Equals(active.FloorId,op.floorId)||!HasCanonicalAbyssOperationIdentity084(campaign,active)||!HasCompletedEveryAbyssStep(active,op)||active.AppliedStepProofs.Count!=op.steps.Length)return false;
            for(var i=0;i<op.steps.Length;i++)
            {
                var step=op.steps[i];var proof=active.AppliedStepProofs[i];if(proof==null||!StringComparer.Ordinal.Equals(proof.StepId,step.stepId)||proof.RequiresBattle!=step.requiresBattle||state.AppliedReceiptIds.Count(x=>StringComparer.Ordinal.Equals(x,proof.Receipt.ReceiptId))!=1||!campaign.Guild.Development.HasAdventureAuthority(proof.Receipt.ReceiptId))return false;
                ProgressionReceipt022 expected;
                if(step.requiresBattle)
                {
                    if(!StringComparer.Ordinal.Equals(proof.Outcome,"VICTORY")||string.IsNullOrWhiteSpace(proof.BattleFinalStateHash)||string.IsNullOrWhiteSpace(proof.ExistingBattleRewardReceiptId)||string.IsNullOrWhiteSpace(proof.BattleReturnReceiptId)||string.IsNullOrWhiteSpace(proof.BattleRequestId)||string.IsNullOrWhiteSpace(proof.BattleId)||string.IsNullOrWhiteSpace(proof.ReturnCheckpointId))return false;
                    if(string.IsNullOrWhiteSpace(proof.BattleReturnAuthorityId)||!campaign.Guild.Development.HasAdventureAuthority(proof.BattleReturnAuthorityId))return false;
                    expected=CreateAbyssBattleReceipt(active,op,step,proof.BattleFinalStateHash,proof.ExistingBattleRewardReceiptId,proof.BattleReturnAuthorityId);
                    var returnHash=CanonicalJson.Sha256Hex(new{active.OperationInstanceId,step.stepId,RequestId=proof.BattleRequestId,BattleId=proof.BattleId,FinalStateHash=proof.BattleFinalStateHash,Outcome=BattleOutcome.Victory,RewardId=proof.ExistingBattleRewardReceiptId,ReturnCheckpointId=proof.ReturnCheckpointId});var expectedReturn="ABYSS_RETURN022_"+returnHash.Substring(0,24).ToUpperInvariant();
                    if(!StringComparer.Ordinal.Equals(proof.BattleReturnReceiptId,expectedReturn)||city==null||city.AppliedBattleReturnIds.Count(x=>StringComparer.Ordinal.Equals(x,expectedReturn))!=1||campaign.Guild.Development.ClaimedBattleRewardIds.Count(x=>StringComparer.Ordinal.Equals(x,proof.ExistingBattleRewardReceiptId))!=1||!StringComparer.Ordinal.Equals(active.ExistingBattleRewardReceiptId,proof.ExistingBattleRewardReceiptId))return false;
                }
                else
                {
                    if(!string.IsNullOrEmpty(proof.BattleFinalStateHash)||!string.IsNullOrEmpty(proof.ExistingBattleRewardReceiptId)||!string.IsNullOrEmpty(proof.BattleReturnReceiptId)||!string.IsNullOrEmpty(proof.BattleRequestId)||!string.IsNullOrEmpty(proof.BattleId)||!string.IsNullOrEmpty(proof.ReturnCheckpointId)||!string.IsNullOrEmpty(proof.BattleReturnAuthorityId))return false;
                    expected=CreateAbyssNonBattleStepReceipt(active,step,proof.Outcome);
                }
                if(!ReceiptsMatch(proof.Receipt,expected))return false;
            }
            return true;
        }

        static bool TryGetActiveAbyssBattleStep(AbyssOperationState022 active,ICampaignRegistry022 registry,out AbyssOperationDto022 op,out AbyssStepDto022 step,out string error)
        {
            op=null;step=null;error=null;if(active==null){error="CAMPAIGN022_ABYSS_ACTIVE_REQUIRED";return false;}if(!registry.AbyssOperations.TryGetValue(active.OperationDefinitionId,out op)){error="CAMPAIGN022_ABYSS_OPERATION_UNKNOWN";return false;}if(active.CurrentStepIndex<0||active.CurrentStepIndex>=op.steps.Length){error="CAMPAIGN022_ABYSS_STEP_RANGE";return false;}step=op.steps[active.CurrentStepIndex];if(!step.requiresBattle){error="CAMPAIGN022_ABYSS_BATTLE_STEP_REQUIRED";return false;}return true;
        }

        static EncounterLaunchRequest017D CreateAbyssBattleEncounterRequest(CampaignState campaign,AbyssOperationState022 active,AbyssOperationDto022 op,AbyssStepDto022 step)
            =>CreateAbyssBattleEncounterRequest130(campaign,active,op,step,null,null);

        static EncounterLaunchRequest017D CreateAbyssBattleEncounterRequest130(CampaignState campaign,AbyssOperationState022 active,AbyssOperationDto022 op,AbyssStepDto022 step,IReadOnlyList<string> boundAlliedUnionIds,int? boundClearCount)
        {
            var recruitIds=new HashSet<string>((campaign?.Guild?.Recruits??Array.Empty<RecruitState>()).Where(x=>x!=null).Select(x=>x.RecruitId),StringComparer.Ordinal);
            var allied=boundAlliedUnionIds?.ToArray()??(campaign?.Guild?.Unions??Array.Empty<UnionState>()).Where(x=>x!=null&&x.Kind==UnionKind.Normal&&x.MemberRecruitIds.Any(recruitIds.Contains)).Select(x=>x.UnionId).OrderBy(x=>x,StringComparer.Ordinal).Take(10).ToArray();
            var towerProgress=campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019?.Playable020?.Progression022;
            var previousClears=boundClearCount??towerProgress?.AbyssFloors?.FirstOrDefault(x=>StringComparer.Ordinal.Equals(x.FloorId,active.FloorId))?.ClearCount??0;
            var actualThreatFloor098=ActualThreatFloor098(campaign,active);
            var enemyUnionCount=actualThreatFloor098>0?TowerThreatRules098.EnemyUnionCount098(actualThreatFloor098):TowerEnemyUnionCount081(TowerFloorNumber081(active.FloorId),previousClears);
            var threatModifier098=actualThreatFloor098>0?TowerThreatRules098.Modifier098(actualThreatFloor098):TowerThreatModifier089(TowerThreatTier089(TowerFloorNumber081(active.FloorId)));
            var encounterId=string.IsNullOrWhiteSpace(step.bossId)?step.stepId:step.bossId;var objective=(op.displayName??active.OperationDefinitionId)+" — "+(step.title??step.stepId);var objectiveIds=new[]{"OBJECTIVE_ABYSS022",step.stepId}.OrderBy(x=>x,StringComparer.Ordinal).ToArray();var routeModifiers=new[]{"ABYSS_OPERATION_022",active.FloorId,op.kind,threatModifier098}.OrderBy(x=>x,StringComparer.Ordinal).ToArray();var returnCheckpointId="RETURN_ABYSS022_"+active.OperationInstanceId+"_"+step.stepId;
            if(UsesFullTowerParties137(campaign,active))
                routeModifiers=routeModifiers.Concat(new[]{TowerEnemyPartyRules137.Modifier137}).OrderBy(x=>x,StringComparer.Ordinal).ToArray();
            if(UsesTowerScaling138(campaign,active))
                routeModifiers=routeModifiers.Concat(new[]{TowerScalingRules138.Modifier138}).OrderBy(x=>x,StringComparer.Ordinal).ToArray();
            if(UsesTowerEconomy159(campaign,active))
                routeModifiers=routeModifiers.Concat(new[]{TowerEconomy159.Modifier159}).OrderBy(x=>x,StringComparer.Ordinal).ToArray();
            var preBattleStateHash=CanonicalJson.Sha256Hex(new{campaign.CampaignGuid,active.OperationInstanceId,active.OperationDefinitionId,active.FloorId,active.CurrentStepIndex,step.stepId,encounterId,active.CanonicalSeedIdentity});
            var requestHash=CanonicalJson.Sha256Hex(new{active.OperationDefinitionId,active.OperationInstanceId,boardId=active.FloorId,nodeId=step.stepId,encounterId,objective,enemyUnionCount,active.CanonicalSeedIdentity,allied,reserveUnionIds=Array.Empty<string>(),objectiveIds,routeModifiers,supplies=0,fatigue=0,urgency=0,returnCheckpointId,preBattleStateHash});var shortHash=requestHash.Substring(0,24).ToUpperInvariant();
            var battleId=actualThreatFloor098>0?TowerThreatRules098.BattleId098(actualThreatFloor098,shortHash):TowerBattleId081(TowerFloorNumber081(active.FloorId),shortHash);
            return new EncounterLaunchRequest017D("ABYSS_ENCOUNTER022_"+shortHash,active.OperationDefinitionId,active.OperationInstanceId,active.FloorId,step.stepId,encounterId,battleId,objective,enemyUnionCount,active.CanonicalSeedIdentity,allied,Array.Empty<string>(),objectiveIds,routeModifiers,0,0,0,returnCheckpointId,preBattleStateHash);
        }

        public static int TowerEnemyUnionCount081(int floorNumber,int previousClearCount)
        {
            // A result above ten is already saturated. Clamp both inputs before
            // adding so extreme saved/API values cannot wrap back to one Union.
            var boundedFloor=Math.Max(1,Math.Min(19,floorNumber));
            var boundedClears=Math.Max(0,Math.Min(9,previousClearCount));
            return Math.Min(10,1+(boundedFloor-1)/2+boundedClears);
        }

        public static int TowerThreatTier089(int floorNumber)
            =>Math.Max(1,Math.Min(MaximumTowerThreatTier089,floorNumber));

        public static string TowerThreatModifier089(int tier)
            =>"TOWER_THREAT_TIER_"+TowerThreatTier089(tier).ToString("00");

        public static int TowerThreatHpBasisPoints089(int tier)
            =>checked((TowerThreatTier089(tier)-1)*500);

        public static int TowerThreatOffenseBasisPoints089(int tier)
            =>checked((TowerThreatTier089(tier)-1)*300);

        public static bool IsTowerMajorRecruitMilestone089(int totalClears)
            =>totalClears>0&&totalClears%TowerMajorRecruitInterval089==0;

        public static string TowerMajorRecruitStableId089(int totalClears,
            IReadOnlyList<string> pendingLeadIds)
        {
            if(!IsTowerMajorRecruitMilestone089(totalClears))return string.Empty;
            var pending=new HashSet<string>(pendingLeadIds??Array.Empty<string>(),
                StringComparer.Ordinal);
            var start=(totalClears/TowerMajorRecruitInterval089-1)%
                      TowerMajorRecruitRoster089.Length;
            for(var offset=0;offset<TowerMajorRecruitRoster089.Length;offset++)
            {
                var candidate=TowerMajorRecruitRoster089[
                    (start+offset)%TowerMajorRecruitRoster089.Length];
                if(!pending.Contains(candidate))return candidate;
            }
            // A lead is an opportunity, not a second recruit authority. If a player
            // has left every milestone lead pending, retain the deterministic first
            // offer and still receipt the newly earned milestone exactly once.
            return TowerMajorRecruitRoster089[start];
        }

        public static string TowerMajorRecruitReceiptId089(string campaignGuid,
            int totalClears,string completionReceiptId,string recruitStableId)
        {
            if(!IsTowerMajorRecruitMilestone089(totalClears)||
               string.IsNullOrWhiteSpace(campaignGuid)||
               string.IsNullOrWhiteSpace(completionReceiptId)||
               string.IsNullOrWhiteSpace(recruitStableId))return string.Empty;
            var hash=CanonicalJson.Sha256Hex(new
            {
                CampaignGuid=campaignGuid,
                TotalTowerClears=totalClears,
                CompletionReceiptId=completionReceiptId,
                RecruitStableId=recruitStableId,
                Authority="TOWER_MAJOR_RECRUIT_089_V1"
            });
            return "TOWERRECRUIT089_"+hash.Substring(0,24).ToUpperInvariant();
        }

        public static string TowerBattleId081(int floorNumber,string stableHashSuffix)
        {
            if(floorNumber<1||floorNumber>10)throw new ArgumentOutOfRangeException(nameof(floorNumber));
            if(string.IsNullOrWhiteSpace(stableHashSuffix))throw new ArgumentException("Tower battle hash is required.",nameof(stableHashSuffix));
            var floorToken=floorNumber<10?"0"+floorNumber:floorNumber.ToString();
            return "ABYSS_BATTLE022_FLOOR_"+floorToken+"_"+stableHashSuffix.Trim().ToUpperInvariant();
        }

        static int TowerFloorNumber081(string floorId)
        {
            if(string.IsNullOrWhiteSpace(floorId))return 1;
            const string marker="ABYSS_FLOOR_";
            var start=floorId.IndexOf(marker,StringComparison.Ordinal);
            if(start<0)return 1;
            start+=marker.Length;
            var end=floorId.IndexOf('_',start);
            var value=end>start?floorId.Substring(start,end-start):floorId.Substring(start);
            return int.TryParse(value,out var parsed)?Math.Max(1,parsed):1;
        }

        static BattleReturnReceipt017D CreateAbyssBattleReturnReceipt(CampaignState campaign,AbyssOperationState022 active,AbyssStepDto022 step,EncounterLaunchRequest017D request,out string error)
        {
            error=null;var battle=campaign?.Battle;if(battle==null||battle.Phase!=BattlePhase.Resolved||battle.Reward==null||!battle.Reward.Claimed){error="CAMPAIGN022_CLAIM_EXISTING_REWARD_FIRST";return null;}if(!StringComparer.Ordinal.Equals(battle.BattleId,request.BattleId)){error="CAMPAIGN022_ABYSS_BATTLE_ID_INVALID";return null;}if(battle.Outcome!=BattleOutcome.Victory||battle.Reward.Outcome!=BattleOutcome.Victory){error="CAMPAIGN022_ABYSS_BATTLE_VICTORY_REQUIRED";return null;}if(!M2BattleCommandService.HasValidFinalStateHash090(battle)){error="CAMPAIGN022_ABYSS_BATTLE_HASH_INVALID";return null;}if(!campaign.Guild.Development.HasClaimedReward(battle.Reward.RewardId)){error="CAMPAIGN022_ABYSS_EXISTING_REWARD_LINK_REQUIRED";return null;}var hash=CanonicalJson.Sha256Hex(new{active.OperationInstanceId,step.stepId,request.RequestId,battle.BattleId,battle.FinalStateHash,battle.Outcome,battle.Reward.RewardId,request.ReturnCheckpointId});return new BattleReturnReceipt017D("ABYSS_RETURN022_"+hash.Substring(0,24).ToUpperInvariant(),request.RequestId,battle.BattleId,battle.Outcome.ToString(),battle.FinalStateHash,0,0,0,new[]{"OBJECTIVE_ABYSS_BATTLE_RETURNED",step.stepId},Array.Empty<GuildMaterialState017D>(),battle.Reward.RewardId,Array.Empty<RelationshipMemoryState017D>(),0,request.ReturnCheckpointId,false);
        }

        static bool AbyssEncountersMatch(EncounterLaunchRequest017D actual,EncounterLaunchRequest017D expected)=>actual!=null&&expected!=null&&StringComparer.Ordinal.Equals(CanonicalJson.Serialize(actual),CanonicalJson.Serialize(expected));
        static bool AbyssBattleReturnsMatch(BattleReturnReceipt017D actual,BattleReturnReceipt017D expected)=>actual!=null&&expected!=null&&StringComparer.Ordinal.Equals(CanonicalJson.Serialize(actual),CanonicalJson.Serialize(expected));

        static ProgressionReceipt022 CreateAbyssBattleReceipt(AbyssOperationState022 active,AbyssOperationDto022 op,AbyssStepDto022 step,BattleState battle,string battleReturnAuthorityId)
        {
            return CreateAbyssBattleReceipt(active,op,step,battle.FinalStateHash,battle.Reward.RewardId,battleReturnAuthorityId);
        }

        static ProgressionReceipt022 CreateAbyssBattleReceipt(AbyssOperationState022 active,AbyssOperationDto022 op,AbyssStepDto022 step,string finalStateHash,string rewardId,string battleReturnAuthorityId)
        {
            var hash=CanonicalJson.Sha256Hex(new{active.OperationInstanceId,step.stepId,FinalStateHash=finalStateHash,RewardId=rewardId,BattleReturnAuthority=battleReturnAuthorityId});
            return new ProgressionReceipt022("ABYSSREC022_"+hash.Substring(0,24).ToUpperInvariant(),active.OperationDefinitionId,"VICTORY",hash,op.guildXp,op.hallXp,op.summonResonance,op.rewardMaterialIds,0);
        }

        static ProgressionReceipt022 CreateAbyssOperationCompletionReceipt(AbyssOperationState022 active,AbyssOperationDto022 op)
        {
            var completedStepIds=op.steps.Select(x=>x.stepId).ToArray();
            var hash=CanonicalJson.Sha256Hex(new
            {
                active.OperationInstanceId,
                active.OperationDefinitionId,
                active.FloorId,
                completedStepIds,
                active.CanonicalSeedIdentity,
                active.ExistingBattleRewardReceiptId,
                outcome="OPERATION_COMPLETE"
            });
            return new ProgressionReceipt022("ABYSSREC022_"+hash.Substring(0,24).ToUpperInvariant(),active.OperationDefinitionId,"OPERATION_COMPLETE",hash,op.guildXp,op.hallXp,op.summonResonance,op.rewardMaterialIds,1);
        }

        static bool ReceiptsMatch(ProgressionReceipt022 actual,ProgressionReceipt022 expected)
        {
            if(actual==null||expected==null||
               !StringComparer.Ordinal.Equals(actual.ReceiptId,expected.ReceiptId)||
               !StringComparer.Ordinal.Equals(actual.SourceId,expected.SourceId)||
               !StringComparer.Ordinal.Equals(actual.Outcome,expected.Outcome)||
               !StringComparer.Ordinal.Equals(actual.AuthoritativeHash,expected.AuthoritativeHash)||
               actual.GuildXp!=expected.GuildXp||actual.HallXp!=expected.HallXp||
               actual.SummonResonance!=expected.SummonResonance||actual.AppliedVersion!=expected.AppliedVersion||
               actual.MaterialIds.Count!=expected.MaterialIds.Count)return false;
            for(var i=0;i<actual.MaterialIds.Count;i++)if(!StringComparer.Ordinal.Equals(actual.MaterialIds[i],expected.MaterialIds[i]))return false;
            return true;
        }

        static string ValidateCovenantAuthority(CampaignState campaign,GuildCityStrategicState017H strategic,CampaignProgressionState022 state,ICampaignRegistry022 registry,CovenantDto022 def,out string gateReceiptId)
        {
            gateReceiptId=string.Empty;
            var requirements=def.requirements??Array.Empty<string>();var requiredLaws=new[]{"XP offering","moral trial","public need","compatible negotiator","Abyss recognition","voluntary acceptance"};
            if(def.sentientOwnershipAllowed||!def.voluntaryAcceptanceRequired||!StringComparer.Ordinal.Equals(def.standardControl,"STORY_AND_COVENANT_FORECASTS")||def.trustMaximum<=0||def.trustMaximum>100||requiredLaws.Any(x=>!requirements.Contains(x)))return "CAMPAIGN022_COVENANT_AUTHORITY_LAW_INVALID";
            if(!strategic.StoryGates.Contains(def.requiredStoryGate))return "CAMPAIGN022_COVENANT_STORY_GATE_REQUIRED";
            if(HighestClearedAbyssFloor(state,registry)<def.minimumAbyssFloor)return "CAMPAIGN022_COVENANT_ABYSS_RECOGNITION_REQUIRED";
            if(StringComparer.Ordinal.Equals(def.requiredStoryGate,GreatCovenantsStoryGate)&&!TryGetCanonicalGreatCovenantGateReceipt(campaign,strategic,state,registry,out gateReceiptId))return "CAMPAIGN022_COVENANT_STORY_GATE_AUTHORITY_REQUIRED";
            return null;
        }

        static bool TryGetCanonicalGreatCovenantGateReceipt(CampaignState campaign,GuildCityStrategicState017H strategic,CampaignProgressionState022 state,ICampaignRegistry022 registry,out string receiptId)
        {
            receiptId=string.Empty;if(campaign?.Guild?.Development==null||strategic==null||state==null||registry==null||!ValidateAbyssAuthority084(campaign,registry,state))return false;var floor=registry.Floors.Values.FirstOrDefault(x=>x.floor==10&&StringComparer.Ordinal.Equals(x.floorId,"ABYSS_FLOOR_010_AEGIS_GATE"));if(floor==null)return false;
            var floors=state.AbyssFloors.Where(x=>StringComparer.Ordinal.Equals(x.FloorId,floor.floorId)).ToArray();if(floors.Length!=1)return false;var progress=floors[0];var proof=progress.FirstClearProof;if(progress.ClearCount<=0||!progress.FirstClearApplied||proof==null)return false;
            if(!StringComparer.Ordinal.Equals(proof.CampaignGuid,campaign.CampaignGuid)||proof.CampaignSeed!=campaign.CampaignSeed||!StringComparer.Ordinal.Equals(proof.FloorId,floor.floorId)||!registry.AbyssOperations.TryGetValue(proof.OperationDefinitionId,out var op)||!StringComparer.Ordinal.Equals(op.floorId,floor.floorId))return false;
            if(ValidateAbyssOperationLaw(registry,op,out var authoredFloor)!=null||authoredFloor==null||!StringComparer.Ordinal.Equals(authoredFloor.floorId,floor.floorId))return false;
            if(proof.AppliedReceiptCountAtBegin+op.steps.Length+1>state.AppliedReceiptIds.Count||proof.CompletedStepIds.Count!=op.steps.Length||proof.StepReceipts.Count!=op.steps.Length)return false;for(var i=0;i<op.steps.Length;i++)if(!StringComparer.Ordinal.Equals(proof.CompletedStepIds[i],op.steps[i].stepId))return false;
            var reconstructed=new AbyssOperationState022(proof.OperationInstanceId,proof.OperationDefinitionId,proof.FloorId,op.steps.Length,AbyssOperationStatus022.ReadyToFinalize,proof.CompletedStepIds,proof.CompletionReceipt,proof.ExistingBattleRewardReceiptId,proof.CanonicalSeedIdentity,proof.AppliedReceiptCountAtBegin,proof.StepReceipts,proof.CommittedOperationOrdinal,proof.PreviousAuthorityHash,proof.BeginAuthorityHash,proof.AlliedRosterIdentity,proof.AlliedRecruitIds,proof.FloorClearCountAtBegin);
            if(!HasCanonicalCompletedAbyssStepProof(campaign,campaign.Guild.GuildCity,state,reconstructed,op))return false;var expectedCompletion=CreateAbyssOperationCompletionReceipt(reconstructed,op);if(!ReceiptsMatch(proof.CompletionReceipt,expectedCompletion)||!StringComparer.Ordinal.Equals(expectedCompletion.Outcome,"OPERATION_COMPLETE")||expectedCompletion.AppliedVersion!=1)return false;
            var expectedGateReceiptId=expectedCompletion.ReceiptId;receiptId=expectedGateReceiptId;if(!expectedGateReceiptId.StartsWith("ABYSSREC022_",StringComparison.Ordinal)||progress.AppliedReceiptIds.Count(x=>StringComparer.Ordinal.Equals(x,expectedGateReceiptId))!=1||state.AppliedReceiptIds.Count(x=>StringComparer.Ordinal.Equals(x,expectedGateReceiptId))!=1||strategic.AppliedStrategicReceiptIds.Count(x=>StringComparer.Ordinal.Equals(x,expectedGateReceiptId))!=1||campaign.Guild.Development.ClaimedBattleRewardIds.Count(x=>StringComparer.Ordinal.Equals(x,expectedGateReceiptId))!=1)return false;
            if(strategic.StoryGates.Count(x=>StringComparer.Ordinal.Equals(x,GreatCovenantsStoryGate))!=1||state.AboveGroundChangeIds.Count(x=>StringComparer.Ordinal.Equals(x,floor.firstClearAboveGroundChangeId))!=1)return false;
            return true;
        }

        static bool HasCanonicalCovenantTrialState(CampaignState campaign,CampaignProgressionState022 state,CovenantDto022 def,CovenantProgressState022 current,string gateReceiptId)
        {
            if(current==null||state.Covenants.Count(x=>StringComparer.Ordinal.Equals(x.CovenantId,def.covenantId))>1||current.TrialProgress<0||current.TrialProgress>100||current.TrialProgress%CovenantTrialStageProgress!=0)return false;
            var completed=current.TrialProgress/CovenantTrialStageProgress;var expectedTrust=Math.Min(def.trustMaximum,completed*CovenantTrialTrustPerStage);if(current.Trust!=expectedTrust)return false;
            var trialReceipts=current.AppliedReceiptIds.Where(x=>x.StartsWith("COVTRIALREC022_",StringComparison.Ordinal)).ToArray();if(trialReceipts.Length!=completed)return false;
            for(var stage=1;stage<=completed;stage++){var receipt=CovenantTrialReceiptId(campaign,def,stage,gateReceiptId);if(current.AppliedReceiptIds.Count(x=>StringComparer.Ordinal.Equals(x,receipt))!=1||state.AppliedReceiptIds.Count(x=>StringComparer.Ordinal.Equals(x,receipt))!=1)return false;}
            var acceptanceReceipts=current.AppliedReceiptIds.Where(x=>x.StartsWith("COVACCEPTREC022_",StringComparison.Ordinal)).ToArray();
            if(current.Status==CovenantStatus022.Accepted){var receipt=CovenantAcceptanceReceiptId(campaign,def,gateReceiptId);return completed==4&&acceptanceReceipts.Length==1&&current.AppliedReceiptIds.Count==completed+1&&current.AppliedReceiptIds.Count(x=>StringComparer.Ordinal.Equals(x,receipt))==1&&state.AppliedReceiptIds.Count(x=>StringComparer.Ordinal.Equals(x,receipt))==1;}
            if(current.AppliedReceiptIds.Count!=completed)return false;
            if(acceptanceReceipts.Length!=0||current.Status==CovenantStatus022.Locked)return false;
            if(completed==0&&current.Status!=CovenantStatus022.TrialAvailable&&current.Status!=CovenantStatus022.Refused)return false;
            if(completed>0&&current.Status!=CovenantStatus022.TrialActive&&current.Status!=CovenantStatus022.Refused)return false;
            return true;
        }

        static string CovenantTrialReceiptId(CampaignState campaign,CovenantDto022 def,int stage,string gateReceiptId)
        {
            var requirement=def.requirements!=null&&stage>0&&stage<=def.requirements.Length?def.requirements[stage-1]:string.Empty;var hash=CanonicalJson.Sha256Hex(new{campaign.CampaignGuid,def.covenantId,greatCovenantGateReceiptId=gateReceiptId,trialStage=stage,authoredRequirement=requirement,progress=CovenantTrialStageProgress,def.requiredStoryGate,def.minimumAbyssFloor,def.standardControl,def.voluntaryAcceptanceRequired});return "COVTRIALREC022_"+hash.Substring(0,24).ToUpperInvariant();
        }

        static string CovenantAcceptanceReceiptId(CampaignState campaign,CovenantDto022 def,string gateReceiptId)
        {
            var stages=Enumerable.Range(1,4).Select(x=>CovenantTrialReceiptId(campaign,def,x,gateReceiptId)).ToArray();var hash=CanonicalJson.Sha256Hex(new{campaign.CampaignGuid,def.covenantId,greatCovenantGateReceiptId=gateReceiptId,stages,def.requiredStoryGate,def.minimumAbyssFloor,def.standardControl,acceptance="VOLUNTARY"});return "COVACCEPTREC022_"+hash.Substring(0,24).ToUpperInvariant();
        }

        static int HighestClearedAbyssFloor(CampaignProgressionState022 state,ICampaignRegistry022 registry)=>state.AbyssFloors.Where(x=>x.ClearCount>0&&registry.Floors.ContainsKey(x.FloorId)).Select(x=>registry.Floors[x.FloorId].floor).DefaultIfEmpty(0).Max();
        static bool HasExactlyOneSelectedForecastForEveryActiveUnion(BattleState battle)
        {
            if(battle==null||battle.Selections==null)return false;
            var active=battle.PlayerUnions.Where(x=>!x.IsDefeated&&!x.Retreated).Select(x=>x.UnionId).OrderBy(x=>x,StringComparer.Ordinal).ToArray();
            if(battle.Selections.Count!=active.Length||battle.Selections.Select(x=>x.UnionId).Distinct(StringComparer.Ordinal).Count()!=active.Length)return false;
            for(var i=0;i<active.Length;i++)
            {
                var matches=battle.Selections.Where(x=>StringComparer.Ordinal.Equals(x.UnionId,active[i])).ToArray();
                if(matches.Length!=1||battle.CommittedForecasts.Count(x=>StringComparer.Ordinal.Equals(x.UnionId,active[i])&&StringComparer.Ordinal.Equals(x.ForecastId,matches[0].ForecastId))!=1)return false;
            }
            return true;
        }
        static bool HasExactReceiptHistory(string history,string receipt)
        {
            if(string.IsNullOrWhiteSpace(history)||string.IsNullOrWhiteSpace(receipt))return false;
            var values=history.Split(new[]{" | "},StringSplitOptions.RemoveEmptyEntries);
            return values.Any(x=>StringComparer.Ordinal.Equals(x.Trim(),receipt));
        }
        static EquippedInvocationArtifact022 FindEquippedInvocationArtifact(CampaignState campaign,CampaignProgressionState022 state,ICampaignRegistry022 registry,BattleUnionState union,int highestClearedFloor)
        {
            if(campaign?.Guild==null||state==null||registry==null||union==null)return null;
            EquippedInvocationArtifact022 result=null;
            foreach(var member in union.Members.OrderBy(x=>x.MemberId,StringComparer.Ordinal))
            {
                var recruit=campaign.Guild.Recruits.FirstOrDefault(x=>StringComparer.Ordinal.Equals(x.RecruitId,member.MemberId));
                var assignment=recruit?.Equipment?.Find(EquipmentSlotIds.ToolRelic);var item=assignment?.Item;
                if(item==null)continue;
                var tagged=item.EquipmentTags.Count(x=>StringComparer.Ordinal.Equals(x,"INVOCATION_ARTIFACT"));
                SpecialRelicInvocationRules001.TryGet(item.DefinitionId,out var specialRule);
                var baseKnown=registry.ArtifactBases.ContainsKey(item.DefinitionId)||specialRule!=null;
                var stateMatches=state.InvocationArtifacts.Count(x=>StringComparer.Ordinal.Equals(x.InstanceId,item.InstanceId));
                if(tagged==0&&!baseKnown&&stateMatches==0)continue;
                if(result!=null||tagged!=1||stateMatches!=1||item.ConditionBasisPoints<=0)return null;
                var artifact=state.InvocationArtifacts.Single(x=>StringComparer.Ordinal.Equals(x.InstanceId,item.InstanceId));
                var ownedItem=FindUniqueOwnedEquipmentItem(campaign.Guild,item.InstanceId,out _);
                if(ownedItem==null||!InvocationArtifactAuthority022.TryValidateArtifact(registry,artifact,ownedItem,out var artifactBase,out var path,out _))return null;
                if(specialRule!=null&&(!registry.WeaponTracks.TryGetValue(specialRule.EquipmentTrackId,out var specialTrack)||specialTrack==null||
                   !StringComparer.Ordinal.Equals(specialTrack.weaponFamilyId,artifactBase.weaponFamilyId)))return null;
                var stages=path.stages??Array.Empty<ArtifactEvolutionStageDto022>();
                if(artifact.EvolutionStage>0)
                {
                    var stage=stages.SingleOrDefault(x=>x.stage==artifact.EvolutionStage);
                    if(stage==null||artifact.Resonance<stage.resonanceRequired||highestClearedFloor<stage.abyssFloorRequired)return null;
                }
                result=new EquippedInvocationArtifact022(union.UnionId,recruit.RecruitId,artifact,specialRule);
            }
            return result;
        }
        static IReadOnlyList<EquipmentItemState> FindOwnedItemsByDefinition(GuildState guild,string definitionId)
        {
            var result=new List<EquipmentItemState>();
            if(guild==null||string.IsNullOrWhiteSpace(definitionId))return result.AsReadOnly();
            for(var inventoryIndex=0;inventoryIndex<guild.Inventory.Count;inventoryIndex++)
            {
                var item=guild.Inventory[inventoryIndex];
                if(item!=null&&StringComparer.Ordinal.Equals(item.DefinitionId,definitionId))result.Add(item);
            }
            for(var recruitIndex=0;recruitIndex<guild.Recruits.Count;recruitIndex++)
            {
                var assignments=guild.Recruits[recruitIndex]?.Equipment?.Assignments;
                if(assignments==null)continue;
                for(var assignmentIndex=0;assignmentIndex<assignments.Count;assignmentIndex++)
                {
                    var item=assignments[assignmentIndex]?.Item;
                    if(item!=null&&StringComparer.Ordinal.Equals(item.DefinitionId,definitionId))result.Add(item);
                }
            }
            result.Sort((a,b)=>StringComparer.Ordinal.Compare(a.InstanceId,b.InstanceId));
            return result.AsReadOnly();
        }

        static EquipmentItemState FindUniqueOwnedEquipmentItem(GuildState guild,string instanceId,out string error)
        {
            error="CAMPAIGN022_INVOCATION_ITEM_NOT_OWNED";
            if(guild==null||string.IsNullOrWhiteSpace(instanceId))return null;
            EquipmentItemState result=null;var count=0;
            for(var inventoryIndex=0;inventoryIndex<guild.Inventory.Count;inventoryIndex++)
            {
                var item=guild.Inventory[inventoryIndex];if(item==null||!StringComparer.Ordinal.Equals(item.InstanceId,instanceId))continue;result=item;count++;
            }
            for(var recruitIndex=0;recruitIndex<guild.Recruits.Count;recruitIndex++)
            {
                var assignments=guild.Recruits[recruitIndex]?.Equipment?.Assignments;
                if(assignments==null)continue;
                for(var assignmentIndex=0;assignmentIndex<assignments.Count;assignmentIndex++)
                {
                    var item=assignments[assignmentIndex]?.Item;if(item==null||!StringComparer.Ordinal.Equals(item.InstanceId,instanceId))continue;result=item;count++;
                }
            }
            if(count==1)return result;if(count>1)error="CAMPAIGN022_INVOCATION_ITEM_AMBIGUOUS";return null;
        }
        static bool IsCanonicalEcho(EchoDto022 echo)=>echo!=null&&!string.IsNullOrWhiteSpace(echo.echoId)&&!string.IsNullOrWhiteSpace(echo.displayName)&&!string.IsNullOrWhiteSpace(echo.description)&&!echo.directIndividualSelection&&!echo.realMoneyGacha&&echo.unlockFloor>0&&echo.sharedApCost>0&&echo.personalMpCost>0&&IsInvocationRole(echo.forecastCategory);
        static bool IsInvocationRole(string role)
        {
            switch((role??string.Empty).ToUpperInvariant())
            {
                case "COMBAT":case "MYSTIC":case "RESTORATION":case "WARDING":case "GUARD":case "SUPPORT":case "TACTICAL":return true;
                default:return false;
            }
        }
        static bool EchoCategoryMatchesForecast(string category,BattleForecastState forecast)
        {
            if(forecast==null||string.IsNullOrWhiteSpace(category))return false;
            switch(category.ToUpperInvariant())
            {
                case "COMBAT":return StringComparer.Ordinal.Equals(forecast.CommandId,"CMD_ALL_OUT")||StringComparer.Ordinal.Equals(forecast.CommandId,"CMD_BALANCED")||forecast.MemberActions.Any(x=>x.Kind==BattleActionKind.Martial);
                case "WARDING":return StringComparer.Ordinal.Equals(forecast.CommandId,"CMD_GUARD")||forecast.MemberActions.Any(x=>x.Kind==BattleActionKind.Guard||StringComparer.OrdinalIgnoreCase.Equals(x.Discipline,"Warding")||StringComparer.OrdinalIgnoreCase.Equals(x.Discipline,"Guard"));
                case "GUARD":return StringComparer.Ordinal.Equals(forecast.CommandId,"CMD_GUARD")||forecast.MemberActions.Any(x=>x.Kind==BattleActionKind.Guard||StringComparer.OrdinalIgnoreCase.Equals(x.Discipline,"Guard"));
                case "RESTORATION":return StringComparer.Ordinal.Equals(forecast.CommandId,"CMD_HEAL")||forecast.MemberActions.Any(x=>x.Kind==BattleActionKind.Restoration||StringComparer.OrdinalIgnoreCase.Equals(x.Discipline,"Restoration"));
                case "MYSTIC":return StringComparer.Ordinal.Equals(forecast.CommandId,"CMD_MYSTIC")||forecast.MemberActions.Any(x=>x.Kind==BattleActionKind.Mystic||StringComparer.OrdinalIgnoreCase.Equals(x.Discipline,"Mystic"));
                case "TACTICAL":return StringComparer.Ordinal.Equals(forecast.CommandId,"CMD_FLANK")||forecast.MemberActions.Any(x=>x.Kind==BattleActionKind.Tactical||StringComparer.OrdinalIgnoreCase.Equals(x.Discipline,"Tactical"));
                case "SUPPORT":return StringComparer.Ordinal.Equals(forecast.CommandId,"CMD_SUPPORT")||StringComparer.Ordinal.Equals(forecast.CommandId,"CMD_AP_RECOVERY")||forecast.MemberActions.Any(x=>x.Kind==BattleActionKind.Recovery||StringComparer.OrdinalIgnoreCase.Equals(x.Discipline,"Support"));
                default:return false;
            }
        }
        static BattleForecastState ApplyEchoToCompleteForecast(BattleForecastState forecast,BattlePlannedActionState invokerAction,EchoForecastInvocation022 invocation)
        {
            var actions=new List<BattlePlannedActionState>();
            for(var i=0;i<forecast.MemberActions.Count;i++)
            {
                var action=forecast.MemberActions[i];
                actions.Add(!ReferenceEquals(action,invokerAction)&&!StringComparer.Ordinal.Equals(action.ActorMemberId,invocation.InvokerMemberId)?action:new BattlePlannedActionState(action.ActorMemberId,action.ActorName,action.TargetUnionId,action.TargetMemberId,action.ArtId,action.ArtName,action.Kind,checked(action.SharedApCost+invocation.SharedApCost),checked(action.PersonalMpCost+invocation.PersonalMpCost),action.PredictedHpDelta,action.PredictedCohesionDelta,action.PredictedFormationDelta,action.Prediction+" · Invocation Echo cost is committed inside this complete Forecast.",action.MeaningfulUse,action.BreakthroughOpportunity,action.AnimationTag,action.Discipline,action.PredictedGrowth,action.BreakthroughTargetArtId,action.BreakthroughTargetArtName,action.AreaActionPlan095));
            }
            var marker=EchoInvocationForecastAuthority022.Marker(invocation);
            var debug=forecast.DeterministicDebugEvidence+"\n"+CanonicalJson.Serialize(new{EchoInvocation=invocation.EchoId,invocation.Role,invocation.ArtifactInstanceId,invocation.InvokerMemberId,invocation.SharedApCost,invocation.PersonalMpCost,invocation.ReceiptId,DirectIndividualSelection=false});
            return new BattleForecastState(forecast.ForecastId,forecast.UnionId,forecast.CommandId,"ECHO — "+invocation.DisplayName+" • "+forecast.CommandName,"Invoke “"+invocation.DisplayName+"” only through this complete Union Forecast. "+forecast.Phrase,forecast.TacticalIntent,forecast.TargetId,forecast.TargetName,actions.AsReadOnly(),checked(forecast.SharedApCost+invocation.SharedApCost),forecast.ApRecovery,checked(forecast.CombinedMpCost+invocation.PersonalMpCost),forecast.ExpectedEffect+" · "+invocation.DisplayName+" invocation committed.",forecast.Risk,forecast.LearningOpportunity+" · "+marker,forecast.FallbackBehavior,forecast.GenerationIdentity,debug);
        }

        sealed class EquippedInvocationArtifact022{public EquippedInvocationArtifact022(string unionId,string recruitId,InvocationArtifactState022 artifact,SpecialRelicInvocationRule001 specialRule){UnionId=unionId;RecruitId=recruitId;Artifact=artifact;SpecialRule=specialRule;}public string UnionId{get;}public string RecruitId{get;}public InvocationArtifactState022 Artifact{get;}public SpecialRelicInvocationRule001 SpecialRule{get;}}
        sealed class EchoInvocationCandidate022{public EchoInvocationCandidate022(EchoDto022 echo,InvocationArtifactState022 artifact,BattleUnionState union,BattleForecastState forecast,BattlePlannedActionState action,SpecialRelicInvocationRule001 specialRule){Echo=echo;Artifact=artifact;Union=union;Forecast=forecast;Action=action;SpecialRule=specialRule;}public EchoDto022 Echo{get;}public InvocationArtifactState022 Artifact{get;}public BattleUnionState Union{get;}public BattleForecastState Forecast{get;}public BattlePlannedActionState Action{get;}public SpecialRelicInvocationRule001 SpecialRule{get;}}

        static Result<CampaignState> ApplyAbyssCompletionReceipt(CampaignState campaign,GuildCityState017D city,GuildCityStrategicState017H strategic,CampaignProgressState019 progress,CampaignPlayableState020 playable,CampaignProgressionState022 state,ICampaignRegistry022 registry,ProgressionReceipt022 receipt, GuildCityRecruitmentService017D towerRecruitment094)
        {
            if(state.AppliedReceiptIds.Contains(receipt.ReceiptId))return Result<CampaignState>.Success(campaign);
            var active=state.ActiveAbyssOperation;
            if(active==null||!registry.AbyssOperations.TryGetValue(active.OperationDefinitionId,out var op)||!registry.Floors.TryGetValue(op.floorId,out var floor))return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_COMPLETION_CONTEXT_INVALID");
            var floors=new List<AbyssFloorProgressState022>(state.AbyssFloors);
            var idx=floors.FindIndex(x=>StringComparer.Ordinal.Equals(x.FloorId,op.floorId));
            var current=idx>=0?floors[idx]:new AbyssFloorProgressState022(op.floorId,0,0,false,false,Array.Empty<string>());
            if(current.AppliedReceiptIds.Contains(receipt.ReceiptId))return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_RECEIPT_STATE_INCONSISTENT");
            var newTowerPolicy094=IsEndlessTowerOperation094(active.OperationInstanceId);
            var actualFloor094=0;
            if(newTowerPolicy094&&!TryReadTowerFloorBinding094(campaign,active.BeginAuthorityHash,
                   active.PreviousAuthorityHash,active.OperationInstanceId,active.OperationDefinitionId,
                   active.FloorId,out actualFloor094))
                return Result<CampaignState>.Failure("TOWER094_FLOOR_BINDING_REQUIRED");
            if(newTowerPolicy094&&TowerHeroRewardRules094.IsRewardFloor(actualFloor094)&&towerRecruitment094==null)
                return Result<CampaignState>.Failure("TOWER094_RECRUITMENT_AUTHORITY_REQUIRED");
            var firstClear=current.ClearCount==0;
            var floorReceipts=new List<string>(current.AppliedReceiptIds){receipt.ReceiptId};
            if(!active.AppliedReceiptCountAtBegin.HasValue||
               !HasCanonicalCompletedAbyssStepProof(campaign,city,state,active,op))
                return Result<CampaignState>.Failure(
                    "CAMPAIGN022_ABYSS_COMPLETION_PROOF_INVALID");
            var completionProof=new AbyssFirstClearProof022(campaign.CampaignGuid,
                campaign.CampaignSeed,active.AppliedReceiptCountAtBegin.Value,
                active.OperationInstanceId,active.OperationDefinitionId,
                active.FloorId,active.CanonicalSeedIdentity,
                op.steps.Select(x=>x.stepId).ToArray(),active.AppliedStepProofs,
                active.ExistingBattleRewardReceiptId,receipt,
                active.CommittedOperationOrdinal,active.PreviousAuthorityHash,
                active.BeginAuthorityHash,active.AlliedRosterIdentity,
                active.AlliedRecruitIds,active.FloorClearCountAtBegin);
            var firstClearProof=firstClear?completionProof:null;
            current=current.With(clearCount:current.ClearCount+1,highestJudgmentTier:Math.Max(current.HighestJudgmentTier,1),firstClearApplied:true,appliedReceiptIds:floorReceipts.AsReadOnly(),firstClearProof:firstClearProof);
            if(idx>=0)floors[idx]=current;else floors.Add(current);
            var changes=new List<string>(state.AboveGroundChangeIds);
            if(!changes.Contains(floor.firstClearAboveGroundChangeId))changes.Add(floor.firstClearAboveGroundChangeId);
            var applied=new List<string>(state.AppliedReceiptIds){receipt.ReceiptId};
            var mats=AddMaterials(playable.WorldMaterials,receipt.MaterialIds,4);
            var authorityEntry=CreateAbyssAuthorityEntry084(state,completionProof);
            if(authorityEntry==null)return Result<CampaignState>.Failure(
                "CAMPAIGN022_ABYSS_AUTHORITY_APPEND_INVALID");
            var authorityEntries=new List<AbyssAuthorityEntry022>(
                state.AbyssAuthorityEntries){authorityEntry};
            var totalClearsAfter=floors.Sum(value=>value.ClearCount);
            var milestoneReceiptId=string.Empty;
            if(!newTowerPolicy094&&IsTowerMajorRecruitMilestone089(totalClearsAfter))
            {
                var worldGate=playable.WorldGate023;
                var recruitStableId=TowerMajorRecruitStableId089(totalClearsAfter,
                    worldGate.ExpeditionRecruitLeadIds089);
                milestoneReceiptId=TowerMajorRecruitReceiptId089(
                    campaign.CampaignGuid,totalClearsAfter,receipt.ReceiptId,
                    recruitStableId);
                if(string.IsNullOrWhiteSpace(milestoneReceiptId)||
                   state.AppliedReceiptIds.Contains(milestoneReceiptId)||
                   campaign.Guild.Development.HasAdventureAuthority(
                       milestoneReceiptId))
                    return Result<CampaignState>.Failure(
                        "CAMPAIGN022_TOWER_MAJOR_RECRUIT_RECEIPT_INVALID");
                if(!campaign.Guild.Development.CanRecordAdventureAuthority(
                       milestoneReceiptId))
                    return Result<CampaignState>.Failure(
                        "CAMPAIGN022_ADVENTURE_AUTHORITY_LEDGER_FULL");
                var leads=new List<string>(worldGate.ExpeditionRecruitLeadIds089);
                if(!leads.Contains(recruitStableId))leads.Add(recruitStableId);
                leads.Sort(StringComparer.Ordinal);
                worldGate=worldGate.With(expeditionRecruitLeadIds089:leads.AsReadOnly(),
                    lastCheckpointId:"tower_major_recruit_089_"+totalClearsAfter);
                playable=playable.With(worldGate023:worldGate,
                    replaceWorldGate023:true,
                    lastCheckpointId:"campaign022_abyss_finalized");
                applied.Add(milestoneReceiptId);
            }
            var next=state.With(abyssFloors:floors.AsReadOnly(),activeAbyssOperation:null,replaceActiveAbyssOperation:true,aboveGroundChangeIds:changes.AsReadOnly(),appliedReceiptIds:applied.AsReadOnly(),summonResonance:checked(state.SummonResonance+receipt.SummonResonance),lastCheckpointId:"campaign022_abyss_finalized",abyssAuthorityEntries:authorityEntries.AsReadOnly(),abyssAuthorityHash:authorityEntry.EntryHash,activeAbyssBeginGrant:null,replaceActiveAbyssBeginGrant:true,activeAbyssStepLedger:Array.Empty<AbyssStepReceiptProof022>());
            if(firstClear&&floor.floor==10&&StringComparer.Ordinal.Equals(floor.floorId,"ABYSS_FLOOR_010_AEGIS_GATE")&&receipt.AppliedVersion==1&&StringComparer.Ordinal.Equals(receipt.Outcome,"OPERATION_COMPLETE"))
            {
                var gates=new List<string>(strategic.StoryGates);if(!gates.Contains(GreatCovenantsStoryGate))gates.Add(GreatCovenantsStoryGate);
                var strategicReceipts=new List<string>(strategic.AppliedStrategicReceiptIds);if(!strategicReceipts.Contains(receipt.ReceiptId))strategicReceipts.Add(receipt.ReceiptId);
                strategic=strategic.With(storyGates:gates.AsReadOnly(),appliedStrategicReceiptIds:strategicReceipts.AsReadOnly(),lastCheckpointId:"campaign022_great_covenants_gate_earned");
            }
            var development=campaign.Guild.Development.RecordBattleReward(receipt.ReceiptId,Math.Max(1,receipt.GuildXp),Math.Max(1,receipt.HallXp));
            if(!string.IsNullOrWhiteSpace(milestoneReceiptId))
                development=development.RecordAdventureAuthority(milestoneReceiptId);
            var guild=campaign.Guild.With(checked(campaign.Guild.TreasuryXp+receipt.GuildXp),campaign.Guild.Recruits,campaign.Guild.Unions,campaign.Guild.Inventory,development);
            var completed=Success(campaign.With(guild,campaign.OpeningFlow),city,strategic,progress,playable.With(worldMaterials:mats,progression022:next,replaceProgression022:true,lastCheckpointId:"campaign022_abyss_finalized"),next);
            if(!completed.IsSuccess)return completed;
            var sssTower=SssTenV4AcquisitionService090.ApplyTowerCompletion090(
                completed.Value,receipt.ReceiptId,newTowerPolicy094?actualFloor094:totalClearsAfter,
                awardLegacyNaturalOffers090:!newTowerPolicy094);
            if(!sssTower.IsSuccess)return sssTower;
            var rewarded094=newTowerPolicy094&&TowerHeroRewardRules094.IsRewardFloor(actualFloor094)
                ?towerRecruitment094.ApplyTowerFloorReward094(sssTower.Value,registry,receipt.ReceiptId,actualFloor094)
                :sssTower;
            if(!rewarded094.IsSuccess||!StringComparer.Ordinal.Equals(op.kind,"TRIAL"))
                return rewarded094;
            return SssTenV4AcquisitionService090.ApplyCompletedAscensionTrial090(
                rewarded094.Value,receipt.ReceiptId,active.AlliedRecruitIds);
        }

        static bool TryContext(CampaignState campaign,out GuildCityState017D city,out GuildCityStrategicState017H strategic,out CampaignProgressState019 progress,out CampaignPlayableState020 playable,out CampaignProgressionState022 state,out string error){city=null;strategic=null;progress=null;playable=null;state=null;error="";if(campaign?.Guild?.GuildCity==null){error="CAMPAIGN022_CAMPAIGN_REQUIRED";return false;}city=campaign.Guild.GuildCity;strategic=city.Strategic017H??GuildCityStrategicState017H.Default();progress=strategic.Campaign019??CampaignProgressState019.Default();playable=progress.Playable020??CampaignPlayableState020.Default();state=playable.Progression022??CampaignProgressionState022.Default();return true;}
        static Result<CampaignState> Success(CampaignState campaign,GuildCityState017D city,GuildCityStrategicState017H strategic,CampaignProgressState019 progress,CampaignPlayableState020 playable,CampaignProgressionState022 state){var p=playable.Progression022==state?playable:playable.With(progression022:state,replaceProgression022:true,lastCheckpointId:state.LastCheckpointId);var pr=progress.With(playable020:p,replacePlayable020:true,lastCheckpointId:state.LastCheckpointId);var s=strategic.With(campaign019:pr,replaceCampaign019:true,lastCheckpointId:state.LastCheckpointId);var c=city.With(strategic017H:s,replaceStrategic017H:true,lastCheckpointId:state.LastCheckpointId);return Result<CampaignState>.Success(campaign.With(campaign.Guild.WithGuildCity(c),campaign.OpeningFlow));}
        static bool TrySpend(IReadOnlyList<WorldMaterialAmount020> current,IReadOnlyList<MaterialCostDto022> costs,out IReadOnlyList<WorldMaterialAmount020> result){var map=(current??Array.Empty<WorldMaterialAmount020>()).ToDictionary(x=>x.MaterialId,x=>x.Amount,StringComparer.Ordinal);if(costs!=null)for(var i=0;i<costs.Count;i++){var cost=costs[i];if(!map.TryGetValue(cost.materialId,out var amount)||amount<cost.amount){result=current;return false;}}if(costs!=null)for(var i=0;i<costs.Count;i++)map[costs[i].materialId]-=costs[i].amount;result=map.Where(x=>x.Value>0).OrderBy(x=>x.Key,StringComparer.Ordinal).Select(x=>new WorldMaterialAmount020(x.Key,x.Value)).ToArray();return true;}
        static IReadOnlyList<WorldMaterialAmount020> AddMaterials(IReadOnlyList<WorldMaterialAmount020> current,IReadOnlyList<string> ids,int amount){var map=(current??Array.Empty<WorldMaterialAmount020>()).ToDictionary(x=>x.MaterialId,x=>x.Amount,StringComparer.Ordinal);if(ids!=null)for(var i=0;i<ids.Count;i++){if(!map.ContainsKey(ids[i]))map[ids[i]]=0;map[ids[i]]=checked(map[ids[i]]+amount);}return map.OrderBy(x=>x.Key,StringComparer.Ordinal).Select(x=>new WorldMaterialAmount020(x.Key,x.Value)).ToArray();}
    }

    internal sealed class EchoForecastInvocation022
    {
        public EchoForecastInvocation022(string echoId,string displayName,string role,string artifactInstanceId,string invokerMemberId,string unionId,string forecastId,int sharedApCost,int personalMpCost,string authoritativeHash,string receiptId)
        {EchoId=echoId;DisplayName=displayName;Role=role;ArtifactInstanceId=artifactInstanceId;InvokerMemberId=invokerMemberId;UnionId=unionId;ForecastId=forecastId;SharedApCost=sharedApCost;PersonalMpCost=personalMpCost;AuthoritativeHash=authoritativeHash;ReceiptId=receiptId;}
        public string EchoId{get;}public string DisplayName{get;}public string Role{get;}public string ArtifactInstanceId{get;}public string InvokerMemberId{get;}public string UnionId{get;}public string ForecastId{get;}public int SharedApCost{get;}public int PersonalMpCost{get;}public string AuthoritativeHash{get;}public string ReceiptId{get;}
    }

    internal static class EchoInvocationForecastAuthority022
    {
        internal const string EventType="SUMMON_ECHO_INVOKED";
        const string MarkerPrefix="ECHO_INVOCATION022=";
        const string PendingPrefix="campaign022_echo_pending:";

        internal static EchoForecastInvocation022 Create(CampaignState campaign,BattleState battle,BattleForecastState forecast,EchoDto022 echo,string artifactInstanceId,string invokerMemberId)
        {
            var role=(echo.forecastCategory??string.Empty).ToUpperInvariant();var hash=Hash(campaign,battle,forecast,echo.echoId,role,artifactInstanceId,invokerMemberId,echo.sharedApCost,echo.personalMpCost);
            return new EchoForecastInvocation022(echo.echoId,echo.displayName,role,artifactInstanceId,invokerMemberId,forecast.UnionId,forecast.ForecastId,echo.sharedApCost,echo.personalMpCost,hash,"ECHOREC022_"+hash.Substring(0,24).ToUpperInvariant());
        }

        internal static string Marker(EchoForecastInvocation022 invocation)=>MarkerPrefix+string.Join("|",new[]{invocation.EchoId,invocation.Role,invocation.ArtifactInstanceId,invocation.InvokerMemberId,invocation.UnionId,invocation.ForecastId,invocation.SharedApCost.ToString(),invocation.PersonalMpCost.ToString(),invocation.AuthoritativeHash,invocation.ReceiptId});
        internal static string PendingCheckpoint(string receiptId)=>PendingPrefix+receiptId;
        internal static string EventText(string unionDisplayName,EchoForecastInvocation022 invocation)=>(unionDisplayName??invocation.UnionId)+" invokes "+invocation.EchoId+" through the selected complete Union Forecast; shared AP and personal MP were charged under "+invocation.ReceiptId+".";

        internal static bool TryAuthorize(CampaignState campaign,BattleState battle,BattleForecastState forecast,out EchoForecastInvocation022 invocation)
        {
            invocation=null;if(campaign==null||battle==null||forecast==null||string.IsNullOrWhiteSpace(forecast.LearningOpportunity))return false;
            var markerIndex=forecast.LearningOpportunity.IndexOf(MarkerPrefix,StringComparison.Ordinal);if(markerIndex<0||markerIndex!=forecast.LearningOpportunity.LastIndexOf(MarkerPrefix,StringComparison.Ordinal))return false;
            var encoded=forecast.LearningOpportunity.Substring(markerIndex+MarkerPrefix.Length).Trim();var separator=encoded.IndexOfAny(new[]{' ','·',';'});if(separator>=0)encoded=encoded.Substring(0,separator);
            var parts=encoded.Split('|');if(parts.Length!=10||!KnownRole(parts[1])||!int.TryParse(parts[6],out var ap)||!int.TryParse(parts[7],out var mp)||ap<=0||mp<=0)return false;
            var expectedHash=Hash(campaign,battle,forecast,parts[0],parts[1],parts[2],parts[3],ap,mp);var expectedReceipt="ECHOREC022_"+expectedHash.Substring(0,24).ToUpperInvariant();
            if(!StringComparer.Ordinal.Equals(parts[4],forecast.UnionId)||!StringComparer.Ordinal.Equals(parts[5],forecast.ForecastId)||!RoleMatchesForecast(parts[1],forecast)||!StringComparer.Ordinal.Equals(parts[8],expectedHash)||!StringComparer.Ordinal.Equals(parts[9],expectedReceipt))return false;
            if(battle.Selections.Count(x=>StringComparer.Ordinal.Equals(x.UnionId,forecast.UnionId)&&StringComparer.Ordinal.Equals(x.ForecastId,forecast.ForecastId))!=1)return false;
            var progression=campaign.Guild?.GuildCity?.Strategic017H?.Campaign019?.Playable020?.Progression022;
            if(progression==null||!StringComparer.Ordinal.Equals(progression.LastCheckpointId,PendingCheckpoint(expectedReceipt))||progression.AppliedReceiptIds.Contains(expectedReceipt))return false;
            var action=forecast.MemberActions.FirstOrDefault(x=>StringComparer.Ordinal.Equals(x.ActorMemberId,parts[3]));if(action==null||action.PersonalMpCost<mp||action.SharedApCost<ap)return false;
            invocation=new EchoForecastInvocation022(parts[0],parts[0],parts[1],parts[2],parts[3],parts[4],parts[5],ap,mp,expectedHash,expectedReceipt);return true;
        }

        static bool KnownRole(string role){switch((role??string.Empty).ToUpperInvariant()){case "COMBAT":case "MYSTIC":case "RESTORATION":case "WARDING":case "GUARD":case "SUPPORT":case "TACTICAL":return true;default:return false;}}
        static bool RoleMatchesForecast(string role,BattleForecastState forecast)
        {
            switch((role??string.Empty).ToUpperInvariant())
            {
                case "COMBAT":return StringComparer.Ordinal.Equals(forecast.CommandId,"CMD_ALL_OUT")||StringComparer.Ordinal.Equals(forecast.CommandId,"CMD_BALANCED")||forecast.MemberActions.Any(x=>x.Kind==BattleActionKind.Martial);
                case "MYSTIC":return StringComparer.Ordinal.Equals(forecast.CommandId,"CMD_MYSTIC")||forecast.MemberActions.Any(x=>x.Kind==BattleActionKind.Mystic||StringComparer.OrdinalIgnoreCase.Equals(x.Discipline,"Mystic"));
                case "RESTORATION":return StringComparer.Ordinal.Equals(forecast.CommandId,"CMD_HEAL")||forecast.MemberActions.Any(x=>x.Kind==BattleActionKind.Restoration||StringComparer.OrdinalIgnoreCase.Equals(x.Discipline,"Restoration"));
                case "WARDING":return StringComparer.Ordinal.Equals(forecast.CommandId,"CMD_GUARD")||forecast.MemberActions.Any(x=>StringComparer.OrdinalIgnoreCase.Equals(x.Discipline,"Warding"));
                case "GUARD":return StringComparer.Ordinal.Equals(forecast.CommandId,"CMD_GUARD")||forecast.MemberActions.Any(x=>x.Kind==BattleActionKind.Guard||StringComparer.OrdinalIgnoreCase.Equals(x.Discipline,"Guard"));
                case "SUPPORT":return StringComparer.Ordinal.Equals(forecast.CommandId,"CMD_SUPPORT")||StringComparer.Ordinal.Equals(forecast.CommandId,"CMD_AP_RECOVERY")||forecast.MemberActions.Any(x=>x.Kind==BattleActionKind.Recovery||StringComparer.OrdinalIgnoreCase.Equals(x.Discipline,"Support"));
                case "TACTICAL":return StringComparer.Ordinal.Equals(forecast.CommandId,"CMD_FLANK")||forecast.MemberActions.Any(x=>x.Kind==BattleActionKind.Tactical||StringComparer.OrdinalIgnoreCase.Equals(x.Discipline,"Tactical"));
                default:return false;
            }
        }
        static string Hash(CampaignState campaign,BattleState battle,BattleForecastState forecast,string echoId,string role,string artifactInstanceId,string invokerMemberId,int sharedApCost,int personalMpCost)=>CanonicalJson.Sha256Hex(new{campaign.CampaignGuid,battle.BattleId,battle.Round,forecast.ForecastId,forecast.GenerationIdentity,forecast.UnionId,echoId,role,artifactInstanceId,invokerMemberId,sharedApCost,personalMpCost,control="COMPLETE_UNION_FORECAST_ONLY"});
    }
}
