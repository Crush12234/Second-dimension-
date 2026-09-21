using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SecondDimension.Core;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.GuildCity017H;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.SSSTenV4;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Presentation.Campaign022
{
    public interface ITowerManualAsyncTransition117
    {
        Task<M1CommandResult> PendingTowerManualTransition117 { get; }
        bool PendingTowerManualStartsBattle117 { get; }
        bool TowerManualIsSaving117 { get; }
        Task<M1CommandResult> BankTowerVictoryAsync117();
        Task<M1CommandResult> StartTowerBattleAsync117();
        void CancelTowerManualBeforeSave117();
    }
}

namespace SecondDimension.Presentation
{
    public sealed partial class M1RuntimeCoordinator : Campaign022.ITowerManualAsyncTransition117
    {
        enum TowerManualMode117 { Auto, Bank, Start, Claim, EquipmentHero, EquipmentUnions, EquipmentUndo, RestartAfterPartyDefeat130 }
        public Task<M1CommandResult> PendingTowerManualTransition117 =>
            (_towerWork116?.Mode == TowerManualMode117.Bank || _towerWork116?.Mode == TowerManualMode117.Start) ? _towerTask116 : null;
        public bool PendingTowerManualStartsBattle117 => _towerWork116?.Mode == TowerManualMode117.Start;
        public bool TowerManualIsSaving117 => PendingTowerManualTransition117 != null && _towerWork116?.IsSaving == true;
        public void CancelTowerManualBeforeSave117()
        { if (_towerWork116?.Mode == TowerManualMode117.Bank || _towerWork116?.Mode == TowerManualMode117.Start) _towerWork116.CancelBeforeWrite(); }
        public Task<M1CommandResult> BankTowerVictoryAsync117() => BeginTowerManual117(TowerManualMode117.Bank);
        public Task<M1CommandResult> StartTowerBattleAsync117() => BeginTowerManual117(TowerManualMode117.Start);

        Task<M1CommandResult> BeginTowerManual117(TowerManualMode117 mode)
        {
            if (Thread.CurrentThread.ManagedThreadId != _coordinatorThread116 || SynchronizationContext.Current == null)
                return Task.FromResult(M1CommandResult.Failure("Tower continuation requires the game thread."));
            if (TowerWriteBusy116()) return Task.FromResult(M1CommandResult.Failure(TowerBusy116));
            var registry = Registry022();
            var work = new TowerWork116(this, registry, mode);
            lock (TowerPathGate116)
            {
                if (TowerPathOwners116.ContainsKey(work.SavePath))
                    return Task.FromResult(M1CommandResult.Failure(TowerBusy116));
                TowerPathOwners116.Add(work.SavePath, work);
            }
            _towerWork116 = work;
            _towerTask116 = RunTowerAutoTransaction116(work, UnityEngine.Application.exitCancellationToken);
            return _towerTask116;
        }

        static TowerPrepared116 BuildTowerCandidateForMode117(TowerWork116 work)
        {
            work.ThrowIfCanceled();
            switch (work.Mode)
            {
                case TowerManualMode117.Bank: return BuildTowerBank117(work);
                case TowerManualMode117.Start: return BuildTowerStart117(work);
                case TowerManualMode117.Claim: return BuildTowerClaim120(work);
                case TowerManualMode117.RestartAfterPartyDefeat130: return BuildTowerRestart130(work);
                case TowerManualMode117.EquipmentHero:
                case TowerManualMode117.EquipmentUnions:
                case TowerManualMode117.EquipmentUndo: return BuildAutoEquipmentCandidate123(work);
                default: return BuildTowerCandidate116(work);
            }
        }

        static TowerPrepared116 RejectedTower117(string error) => new TowerPrepared116(Result<CampaignState>.Failure(error));

        static TowerPrepared116 BuildTowerBank117(TowerWork116 work)
        {
            var source = work.Source;
            if (ProgressionState081(source).ActiveAbyssOperation == null)
                return RejectedTower117("No Tower floor is waiting to be banked.");
            if (source.Battle == null || source.Battle.Phase != BattlePhase.Resolved || source.Battle.Outcome != BattleOutcome.Victory)
                return RejectedTower117("Finish the Tower battle before banking its victory.");
            if (source.Battle.Reward?.Claimed != true)
                return RejectedTower117("Claim the battle reward before banking this Tower floor.");
            var banked = AdvanceTowerBoundary116(source, work);
            if (!banked.IsSuccess) return new TowerPrepared116(banked);
            if (ProgressionState081(banked.Value).ActiveAbyssOperation != null)
                return RejectedTower117("The current Tower floor still needs its battle. No reward was banked.");
            var message = TowerCompletionCopy117(source, banked.Value, work);
            return PrepareTowerCandidateFinal117(source, banked.Value, work, null, message);
        }

        static TowerPrepared116 BuildTowerStart117(TowerWork116 work)
        {
            // Keep the exact synchronous Start110 guards and prebattle-only prefix.
            var source = work.Source;
            var city = source?.Guild?.GuildCity;
            if (city == null) return RejectedTower117("The Tower is unavailable.");
            if (source.Battle != null && (source.Battle.Phase != BattlePhase.Resolved || source.Battle.Outcome == BattleOutcome.InProgress))
                return RejectedTower117("Finish the active battle before starting another Tower floor.");
            if (source.Battle?.Reward != null && !source.Battle.Reward.Claimed)
                return RejectedTower117("Claim the battle reward before starting another Tower floor.");
            if (city.PendingBattleReturn != null)
                return RejectedTower117("Finish banking the current battle before starting another Tower floor.");
            var candidate = source;
            var active = ProgressionState081(candidate).ActiveAbyssOperation;
            if (active == null)
            {
                var begun = work.Commands.BeginTowerFloor094(candidate, work.Registry);
                if (!begun.IsSuccess) return new TowerPrepared116(begun);
                candidate = begun.Value;
                active = ProgressionState081(candidate).ActiveAbyssOperation;
            }
            if (active.Status == AbyssOperationStatus022.Active)
            {
                if (!work.Registry.AbyssOperations.TryGetValue(active.OperationDefinitionId, out var operation))
                    return RejectedTower117("CAMPAIGN022_ABYSS_OPERATION_UNKNOWN");
                if (active.CurrentStepIndex < 0 || active.CurrentStepIndex >= operation.steps.Length)
                    return RejectedTower117("CAMPAIGN022_ABYSS_STEP_RANGE");
                if (!string.IsNullOrWhiteSpace(active.ExistingBattleRewardReceiptId))
                    return RejectedTower117("Bank the current Tower victory before starting another floor.");
                for (var index = 0; index < active.CurrentStepIndex; index++)
                    if (operation.steps[index].requiresBattle)
                        return RejectedTower117("Bank the current Tower victory before starting another floor.");
                var prepared = AdvanceTowerBoundary116(candidate, work);
                if (!prepared.IsSuccess) return new TowerPrepared116(prepared);
                candidate = prepared.Value;
                active = ProgressionState081(candidate).ActiveAbyssOperation;
                if (active == null || active.Status != AbyssOperationStatus022.Active)
                    return RejectedTower117("The Tower floor is not ready for battle.");
            }
            else if (active.Status != AbyssOperationStatus022.AwaitingBattle || active.PendingReceipt != null || city.PendingEncounter == null)
                return RejectedTower117("Bank the current Tower victory before starting another floor.");
            var encounter = work.Commands.CommitAbyssBattleEncounter(candidate, work.Registry);
            if (!encounter.IsSuccess) return new TowerPrepared116(encounter);
            var started = work.Bridge.StartCertifiedEncounter(encounter.Value, work.Battles, work.Combat, work.Enemies);
            if (!started.IsSuccess) return new TowerPrepared116(started);
            if (work.GuildContent == null) return RejectedTower117("Guild/city authority is unavailable. " + work.StartupNotice117);
            return PrepareTowerCandidateFinal117(source, started.Value, work, null, "Tower battle started.");
        }

        static TowerPrepared116 PrepareTowerCandidateFinal117(CampaignState source, CampaignState candidate,
            TowerWork116 work, Campaign022.TowerSavedReward110 display, string message = null)
        {
            work.ThrowIfCanceled();
            if (work.Starter != null) candidate = work.Starter.ApplyToNewRecruits(source, candidate);
            candidate = SynchronizePeopleCandidate116(candidate);
            var automatic = SssAutomaticRewards107.ApplyReady(candidate);
            if (!automatic.IsSuccess) return new TowerPrepared116(automatic);
            candidate = SecondDimension.Gameplay.M1.UnionBattlePlanRules132.PromoteWhenIdle(automatic.Value);
            if (work.StrategicContent != null && candidate?.Guild?.GuildCity != null)
            {
                var synchronized = GuildCityStoryGateService017H.SynchronizeDerivedGates(candidate);
                if (!synchronized.IsSuccess) return new TowerPrepared116(synchronized);
                candidate = synchronized.Value;
            }
            work.ThrowIfCanceled();
            return new TowerPrepared116(Result<CampaignState>.Success(candidate), display, message);
        }

        static string TowerCompletionCopy117(CampaignState before, CampaignState completed, TowerWork116 work)
        {
            var active = ProgressionState081(before).ActiveAbyssOperation;
            if (active?.OperationInstanceId.StartsWith(CampaignProgressionCommandService022.TowerOperationPrefix094, StringComparison.Ordinal) == true)
            {
                if (work.Recruitment != null)
                {
                    var reward = work.Recruitment.DescribeLatestTowerHeroReward094(completed, work.Registry);
                    if (reward.IsSuccess && reward.Value != null)
                        return "Tower floor " + reward.Value.ActualFloor + " cleared. " + TowerSavedHeroRewardCopy094(reward.Value);
                }
                return "Tower floor cleared. Rewards are banked and the next floor is unlocked.";
            }
            return CampaignProgressionCommandService022.IsTowerMajorRecruitMilestone089(
                ProgressionState081(completed).AbyssFloors.Sum(floor => floor.ClearCount))
                ? "Tower floor cleared. Your legacy S-rank recruit lead is waiting at Recruitment."
                : "Tower floor cleared. Rewards are banked and the next floor is unlocked.";
        }
    }
}
