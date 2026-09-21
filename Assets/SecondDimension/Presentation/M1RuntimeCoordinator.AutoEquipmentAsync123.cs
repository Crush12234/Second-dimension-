using System;
using System.Threading;
using System.Threading.Tasks;
using SecondDimension.Core;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Presentation
{
    public interface IAutoEquipmentAsyncCoordinator123
    {
        Task<M1CommandResult> PendingAutoEquipment123 { get; }
        bool AutoEquipmentIsSaving123 { get; }
        Task<M1CommandResult> AutoEquipHeroAsync123(string recruitId);
        Task<M1CommandResult> AutoEquipAllUnionsAsync123();
        Task<M1CommandResult> UndoAutoEquipAsync123();
        void CancelAutoEquipmentBeforeSave123();
    }

    public sealed partial class M1RuntimeCoordinator : IAutoEquipmentAsyncCoordinator123
    {
        const string EquipmentBusy123 = "An equipment update is in progress. Please wait.";
        const string EquipmentCanceled123 = "Equipment update canceled before saving. Your saved equipment is unchanged.";
        internal const string NoEquipmentUpgrade123 = "No compatible upgrades found. Current equipment kept.";

        static bool IsAutoEquipmentMode123(TowerManualMode117 mode) =>
            mode == TowerManualMode117.EquipmentHero || mode == TowerManualMode117.EquipmentUnions ||
            mode == TowerManualMode117.EquipmentUndo;

        public Task<M1CommandResult> PendingAutoEquipment123 =>
            _towerWork116 != null && IsAutoEquipmentMode123(_towerWork116.Mode) ? _towerTask116 : null;
        public bool AutoEquipmentIsSaving123 => PendingAutoEquipment123 != null && _towerWork116.IsSaving;
        public void CancelAutoEquipmentBeforeSave123()
        { if (_towerWork116 != null && IsAutoEquipmentMode123(_towerWork116.Mode)) _towerWork116.CancelBeforeWrite(); }

        public Task<M1CommandResult> AutoEquipHeroAsync123(string recruitId) => BeginAutoEquipment123(TowerManualMode117.EquipmentHero, recruitId);
        public Task<M1CommandResult> AutoEquipAllUnionsAsync123() => BeginAutoEquipment123(TowerManualMode117.EquipmentUnions, null);
        public Task<M1CommandResult> UndoAutoEquipAsync123() => BeginAutoEquipment123(TowerManualMode117.EquipmentUndo, null);

        Task<M1CommandResult> BeginAutoEquipment123(TowerManualMode117 mode, string recruitId)
        {
            if (Thread.CurrentThread.ManagedThreadId != _coordinatorThread116 || SynchronizationContext.Current == null)
                return Task.FromResult(M1CommandResult.Failure("Equipment updates require the game thread."));
            if (TowerWriteBusy116()) return Task.FromResult(M1CommandResult.Failure(EquipmentBusy123));
            if (mode != TowerManualMode117.EquipmentUndo && (_combatContent == null || _starterEquipment094 == null))
                return Task.FromResult(M1CommandResult.Failure("The approved equipment authority is unavailable."));
            // Same path lease and transaction owner as Tower116/117/120. Capture
            // loaded immutable source/content only; equipment needs no Tower registry.
            var work = new TowerWork116(this, null, mode, recruitId);
            lock (TowerPathGate116)
            {
                if (TowerPathOwners116.ContainsKey(work.SavePath))
                    return Task.FromResult(M1CommandResult.Failure(EquipmentBusy123));
                TowerPathOwners116.Add(work.SavePath, work);
            }
            _towerWork116 = work;
            _towerTask116 = RunTowerAutoTransaction116(work, UnityEngine.Application.exitCancellationToken);
            return _towerTask116;
        }

        static TowerPrepared116 BuildAutoEquipmentCandidate123(TowerWork116 work)
        {
            work.ThrowIfCanceled();
            Result<CampaignState> result;
            string message;
            if (work.Mode == TowerManualMode117.EquipmentUndo)
            {
                result = AutoEquipmentService112.Undo(work.Source);
                message = "Auto Equip undone. Previous equipment assignments restored and saved.";
            }
            else
            {
                var service = new AutoEquipmentService112(work.Combat, work.Starter);
                result = work.Mode == TowerManualMode117.EquipmentHero
                    ? service.EquipHero(work.Source, work.EquipmentRecruitId123)
                    : service.EquipAllUnions(work.Source);
                work.ThrowIfCanceled();
                // Preserve112 exactly: no-op retains the actual source/Undo and
                // performs no final synchronization, envelope, store or Changed.
                if (result.IsSuccess && ReferenceEquals(result.Value, work.Source))
                    return new TowerPrepared116(result, message: NoEquipmentUpgrade123, persist: false);
                message = result.IsSuccess ? result.Value.EquipmentUndo112.Summary : "Equipment kept.";
            }
            if (!result.IsSuccess) return new TowerPrepared116(result);
            return PrepareTowerCandidateFinal117(work.Source, result.Value, work, null, message);
        }
    }
}
