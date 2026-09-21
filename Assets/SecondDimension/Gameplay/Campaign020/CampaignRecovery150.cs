using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SecondDimension.Core;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.Campaign020
{
    // A versioned, optional part of the existing atomic save. Only execution slots
    // are parked; the Guild, Tower, wallet and reward ledgers remain live.
    [Serializable]
    public sealed class CampaignRecovery150
    {
        [JsonConstructor]
        public CampaignRecovery150(string campaignGuid, bool paused, int operationOrdinal,
            CampaignOperationCommit019 chapter, CampaignPlayableOperationState020 operation,
            CampaignPlayableBeginGrant020 grant, IReadOnlyList<CampaignStepReceipt020> stepLedger,
            WorldGateOperationState023 worldOperation, IReadOnlyList<WorldGateNodeReceipt023> nodeLedger,
            ContractCommitState017D contract, ExpeditionState017D expedition, BattleState settledBattle)
        {
            if(string.IsNullOrWhiteSpace(campaignGuid) || operationOrdinal < 0 || operation == null ||
               operation.PendingReceipt != null || worldOperation?.PendingReceipt != null ||
               worldOperation?.ExpeditionDeck089?.PendingReceipt != null ||
               settledBattle != null && (settledBattle.Outcome == BattleOutcome.InProgress ||
                   settledBattle.Reward != null && !settledBattle.Reward.Claimed))
                throw new ArgumentException("Only a settled Campaign checkpoint can be parked.");
            CampaignGuid=campaignGuid; Paused=paused; OperationOrdinal=operationOrdinal;
            Chapter=chapter; Operation=operation; Grant=grant;
            StepLedger=new List<CampaignStepReceipt020>(stepLedger ?? Array.Empty<CampaignStepReceipt020>()).AsReadOnly();
            WorldOperation=worldOperation;
            NodeLedger=new List<WorldGateNodeReceipt023>(nodeLedger ?? Array.Empty<WorldGateNodeReceipt023>()).AsReadOnly();
            Contract=contract; Expedition=expedition; SettledBattle=settledBattle;
        }
        public string CampaignGuid {get;}
        public bool Paused {get;}
        public int OperationOrdinal {get;}
        public CampaignOperationCommit019 Chapter {get;}
        public CampaignPlayableOperationState020 Operation {get;}
        public CampaignPlayableBeginGrant020 Grant {get;}
        public IReadOnlyList<CampaignStepReceipt020> StepLedger {get;}
        public WorldGateOperationState023 WorldOperation {get;}
        public IReadOnlyList<WorldGateNodeReceipt023> NodeLedger {get;}
        public ContractCommitState017D Contract {get;}
        public ExpeditionState017D Expedition {get;}
        public BattleState SettledBattle {get;}
        public CampaignRecovery150 WithPaused(bool paused) => new CampaignRecovery150(CampaignGuid,
            paused,OperationOrdinal,Chapter,Operation,Grant,StepLedger,WorldOperation,NodeLedger,
            Contract,Expedition,SettledBattle);
    }

    public static class CampaignRecoveryCommands150
    {
        static bool Unsettled(CampaignState state)
        {
            var city=state.Guild.GuildCity;
            var progress=city.Strategic017H.Campaign019;
            var playable=progress.Playable020;
            return city.PendingEncounter!=null || city.PendingBattleReturn!=null ||
                city.Strategic017H.ActiveDefense!=null || progress.PendingReceipt!=null ||
                playable.ActiveOperation?.PendingReceipt!=null ||
                playable.WorldGate023.ActiveOperation?.PendingReceipt!=null ||
                playable.WorldGate023.ActiveOperation?.ExpeditionDeck089?.PendingReceipt!=null ||
                playable.Progression022.ActiveAbyssOperation!=null ||
                state.Battle!=null && (state.Battle.Outcome==BattleOutcome.InProgress ||
                    state.Battle.Reward!=null && !state.Battle.Reward.Claimed);
        }

        public static Result<CampaignState> Pause(CampaignState state, IWorldGateOperationsCatalog023 catalog)
        {
            if(state?.Guild?.GuildCity==null) return Result<CampaignState>.Failure("CAMPAIGN150_GUILD_REQUIRED");
            if(state.Recovery150?.Paused==true) return Result<CampaignState>.Success(state);
            var city=state.Guild.GuildCity; var progress=city.Strategic017H.Campaign019;
            var playable=progress.Playable020; var gate=playable.WorldGate023;
            if(playable.ActiveOperation==null) return Result<CampaignState>.Success(state);
            if(Unsettled(state)) return Result<CampaignState>.Failure("Finish the current battle or card result before pausing your Campaign.");
            if(gate.ActiveOperation!=null && (gate.ActiveOperation.OperationKind!="CHAPTER" ||
                gate.ActiveOperation.DefinitionId!=playable.ActiveOperation.ChapterId ||
                !CampaignWorldGateCommandService023.ValidateActiveAuthority093(state,catalog,out _)))
                return Result<CampaignState>.Failure("CAMPAIGN150_LINKED_AUTHORITY_INVALID");
            if(gate.ActiveOperation==null && GuildCityExpeditionService017D.HasUnresolvedOperation(city))
                return Result<CampaignState>.Failure("CAMPAIGN150_OTHER_ADVENTURE_ACTIVE");
            var snapshot=new CampaignRecovery150(state.CampaignGuid,true,
                gate.ActiveOperation!=null ? city.ActiveContract.AcceptedOperationOrdinal : city.OperationOrdinal,
                progress.ActiveOperation,playable.ActiveOperation,playable.ActiveOperationGrant,
                playable.ActiveStepLedger,gate.ActiveOperation,gate.ActiveNodeLedger,
                city.ActiveContract,city.Expedition,state.Battle);
            gate=gate.With(activeOperation:null,replaceActiveOperation:true,activeNodeLedger:Array.Empty<WorldGateNodeReceipt023>());
            playable=playable.With(activeOperation:null,replaceActiveOperation:true,
                activeOperationGrant:null,replaceActiveOperationGrant:true,activeStepLedger:Array.Empty<CampaignStepReceipt020>(),
                worldGate023:gate,replaceWorldGate023:true);
            progress=progress.With(activeOperation:null,replaceActiveOperation:true,playable020:playable,replacePlayable020:true);
            city=city.With(activeContract:null,replaceActiveContract:true,expedition:null,replaceExpedition:true,
                strategic017H:city.Strategic017H.With(campaign019:progress,replaceCampaign019:true),replaceStrategic017H:true);
            return Result<CampaignState>.Success(state.WithRecovery150(snapshot).With(state.Guild.WithGuildCity(city),state.OpeningFlow));
        }

        public static Result<CampaignState> Resume(CampaignState state, string chapterId, IWorldGateOperationsCatalog023 catalog)
        {
            var saved=state?.Recovery150;
            if(saved?.Paused!=true) return Result<CampaignState>.Success(state);
            if(saved.CampaignGuid!=state.CampaignGuid || saved.Operation.ChapterId!=chapterId)
                return Result<CampaignState>.Failure("Resume the saved Campaign quest before starting a different one.");
            if(Unsettled(state) || GuildCityExpeditionService017D.HasAnyUnresolvedAdventure084(state,true))
                return Result<CampaignState>.Failure("Finish and bank the current activity before resuming your Campaign.");
            var city=state.Guild.GuildCity; var progress=city.Strategic017H.Campaign019;
            var playable=progress.Playable020;
            if(progress.ActiveChapterId!=chapterId || saved.OperationOrdinal>city.OperationOrdinal)
                return Result<CampaignState>.Failure("CAMPAIGN150_CHECKPOINT_CONFLICT");
            var gate=playable.WorldGate023.With(activeOperation:saved.WorldOperation,replaceActiveOperation:true,activeNodeLedger:saved.NodeLedger);
            playable=playable.With(activeOperation:saved.Operation,replaceActiveOperation:true,
                activeOperationGrant:saved.Grant,replaceActiveOperationGrant:true,activeStepLedger:saved.StepLedger,
                worldGate023:gate,replaceWorldGate023:true);
            progress=progress.With(activeOperation:saved.Chapter,replaceActiveOperation:true,playable020:playable,replacePlayable020:true);
            city=city.With(activeContract:saved.Contract,replaceActiveContract:true,expedition:saved.Expedition,replaceExpedition:true,
                strategic017H:city.Strategic017H.With(campaign019:progress,replaceCampaign019:true),replaceStrategic017H:true);
            var restored=state.WithRecovery150(saved.WithPaused(false)).With(state.Guild.WithGuildCity(city),state.OpeningFlow).WithBattle(saved.SettledBattle);
            if(saved.WorldOperation!=null && !CampaignWorldGateCommandService023.ValidateActiveAuthority093(restored,catalog,out var error))
                return Result<CampaignState>.Failure("CAMPAIGN150_RESTORED_AUTHORITY_INVALID");
            return Result<CampaignState>.Success(restored);
        }

        public static int CommittedWorldOrdinal(CampaignState state)
        {
            var city=state.Guild.GuildCity;
            var active=city.Strategic017H.Campaign019.Playable020.WorldGate023.ActiveOperation;
            var saved=state.Recovery150;
            return saved!=null && !saved.Paused && saved.CampaignGuid==state.CampaignGuid &&
                active!=null && saved.WorldOperation?.OperationId==active.OperationId &&
                saved.OperationOrdinal==city.ActiveContract?.AcceptedOperationOrdinal &&
                saved.OperationOrdinal<=city.OperationOrdinal ? saved.OperationOrdinal :
                SecondDimension.Gameplay.Navigation164.LoopCheckpoint164.WorldOrdinal(state,city.OperationOrdinal);
        }

        // Identity projection only: never replace the live operation counter.
        public static GuildCityState017D WorldIdentityCity(CampaignState state) =>
            state.Guild.GuildCity.With(operationOrdinal:CommittedWorldOrdinal(state));
    }
}
